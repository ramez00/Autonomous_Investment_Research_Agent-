using Microsoft.Extensions.Logging;
using AIRA.Agents.LLM;

namespace AIRA.Agents.Agents;

/// <summary>
/// This Agent responsible for research strategy before analysis,
/// it will create a research plan based on the company and analysis depth,
/// and then pass the plan to other agents for execution
/// Agent responsible for planning and decomposing the research task
/// </summary>
public class PlannerAgent : BaseResearchAgent
{
    private readonly ILlmClient? _llmClient;
    public override string AgentName => "Planner";

    public PlannerAgent(ILlmClient? llmClient, ILogger<PlannerAgent> logger) 
        : base(logger)
    {
        _llmClient = llmClient;
    }

    /// <summary>
    /// Creates a research plan for analyzing a company
    /// </summary>
    public async Task<ResearchPlan> CreateResearchPlanAsync(
        string companyName,
        string symbol,
        string analysisDepth,
        CancellationToken cancellationToken = default)
    {
        // Sanitize inputs to prevent prompt injection
        var sanitizedCompanyName = Tools.ValidationHelper.SanitizeInput(companyName, 200);
        var sanitizedSymbol = Tools.ValidationHelper.SanitizeInput(symbol, 10);
        var sanitizedDepth = Tools.ValidationHelper.SanitizeInput(analysisDepth, 50);
        
        var startTime = DateTime.UtcNow;
        
        await RecordStepAsync(
            $"Starting research planning for {sanitizedCompanyName} ({sanitizedSymbol})",
            $"Analysis depth: {sanitizedDepth}");

        var systemPrompt = @"You are a senior investment research analyst. Your task is to create a structured research plan for analyzing a company.

Based on the company and analysis depth, identify the key areas to investigate:
1. Financial Performance (revenue, profitability, growth trends)
2. Market Position (competitive landscape, market share)
3. Valuation Metrics (P/E, P/B, EV/EBITDA)
4. Recent News and Sentiment
5. Risk Factors

Output a JSON object with the following structure:
{
    ""focusAreas"": [""area1"", ""area2"", ...],
    ""financialMetricsToAnalyze"": [""metric1"", ""metric2"", ...],
    ""newsTopics"": [""topic1"", ""topic2"", ...],
    ""timeframeMonths"": 12,
    ""riskFactorsToConsider"": [""risk1"", ""risk2"", ...]
}";

        var userPrompt = $@"Create a research plan for: {sanitizedCompanyName} (Ticker: {sanitizedSymbol})
Analysis Depth: {sanitizedDepth}

Provide the research plan in JSON format.";

        try
        {
            if (_llmClient == null)
            {
                Logger.LogWarning("No LLM client configured, using default plan");
                return CreateDefaultPlan(sanitizedCompanyName, sanitizedSymbol, sanitizedDepth);
            }

            var content = await _llmClient.CompleteChatAsync(systemPrompt, userPrompt, cancellationToken);
            
            // Parse the response to extract the plan
            var plan = ParseResearchPlan(content, sanitizedCompanyName, sanitizedSymbol, sanitizedDepth);
            
            var duration = DateTime.UtcNow - startTime;
            await RecordStepAsync(
                $"Created research plan with {plan.FocusAreas.Count} focus areas",
                $"Focus areas: {string.Join(", ", plan.FocusAreas)}",
                duration: duration);

            return plan;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating research plan");
            await RecordStepAsync(
                "Failed to create research plan",
                isSuccess: false,
                errorMessage: ex.Message);

            // if there's an error, we can return a default plan.
            // Return a default plan
            return CreateDefaultPlan(sanitizedCompanyName, sanitizedSymbol, sanitizedDepth);
        }
    }


    // Convert the response from AI agent content into a ResearchPlan object
    
    private ResearchPlan ParseResearchPlan(string content, string companyName, string symbol, string depth)
    {
        try
        {
            Logger.LogInformation("Starting Parse research plan from AI response");

            // Use safe JSON parser
            var parsed = Tools.SafeJsonParser.ParseJsonFromText<ResearchPlanJson>(content);
            
            if (parsed != null)
            {
                return new ResearchPlan
                {
                    CompanyName = companyName,
                    Symbol = symbol,
                    AnalysisDepth = depth,
                    FocusAreas = parsed.FocusAreas ?? new List<string>(),
                    FinancialMetrics = parsed.FinancialMetricsToAnalyze ?? new List<string>(),
                    NewsTopics = parsed.NewsTopics ?? new List<string>(),
                    TimeframeMonths = parsed.TimeframeMonths > 0 && parsed.TimeframeMonths <= 60 ? parsed.TimeframeMonths : 12,
                    RiskFactors = parsed.RiskFactorsToConsider ?? new List<string>()
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to parse research plan JSON, using default");
        }
        
        return CreateDefaultPlan(companyName, symbol, depth);
    }

    private static ResearchPlan CreateDefaultPlan(string companyName, string symbol, string depth)
    {
        return new ResearchPlan
        {
            CompanyName = companyName,
            Symbol = symbol,
            AnalysisDepth = depth,
            FocusAreas = new List<string>
            {
                "Financial Performance",
                "Valuation Metrics",
                "Market Sentiment",
                "Growth Prospects",
                "Risk Assessment"
            },
            FinancialMetrics = new List<string>
            {
                "Revenue Growth",
                "Profit Margins",
                "P/E Ratio",
                "Debt to Equity",
                "Free Cash Flow"
            },
            NewsTopics = new List<string>
            {
                "Earnings",
                "Product Launches",
                "Management Changes",
                "Market Competition",
                "Regulatory Issues"
            },
            TimeframeMonths = depth == "deep" ? 24 : depth == "quick" ? 6 : 12,
            RiskFactors = new List<string>
            {
                "Market Risk",
                "Competition",
                "Regulatory Risk",
                "Operational Risk"
            }
        };
    }

    private class ResearchPlanJson
    {
        public List<string>? FocusAreas { get; set; }
        public List<string>? FinancialMetricsToAnalyze { get; set; }
        public List<string>? NewsTopics { get; set; }
        public int TimeframeMonths { get; set; } = 12;
        public List<string>? RiskFactorsToConsider { get; set; }
    }
}

public class ResearchPlan
{
    public required string CompanyName { get; set; }
    public required string Symbol { get; set; }
    public required string AnalysisDepth { get; set; }
    public List<string> FocusAreas { get; set; } = new();
    public List<string> FinancialMetrics { get; set; } = new();
    public List<string> NewsTopics { get; set; } = new();
    public int TimeframeMonths { get; set; } = 12;
    public List<string> RiskFactors { get; set; } = new();
}
