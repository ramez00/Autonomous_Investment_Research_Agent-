using AIRA.Core.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace AIRA.Agents.Agents;

/// <summary>
/// Agent responsible for synthesizing all gathered data into a final investment thesis
/// </summary>
public class SynthesizerAgent : BaseResearchAgent
{
    private readonly ChatClient _chatClient;

    public override string AgentName => "Synthesizer";

    public SynthesizerAgent(ChatClient chatClient, ILogger<SynthesizerAgent> logger) 
        : base(logger)
    {
        _chatClient = chatClient;
    }

    /// <summary>
    /// Synthesizes all research data into a final analysis result
    /// </summary>
    public async Task<AnalysisResult> SynthesizeAsync(
        string companyName,
        string symbol,
        ResearchPlan plan,
        FinancialAnalysis financialAnalysis,
        NewsAnalysis newsAnalysis,
        List<AgentStep> allSteps,
        CancellationToken cancellationToken = default)
    {
        // Sanitize inputs
        var sanitizedCompanyName = Tools.ValidationHelper.SanitizeInput(companyName, 200);
        var sanitizedSymbol = Tools.ValidationHelper.SanitizeInput(symbol, 10);
        
        var startTime = DateTime.UtcNow;
        
        await RecordStepAsync(
            $"Starting synthesis for {sanitizedCompanyName}",
            "Combining financial and news analysis");

        // Build the analysis summary for the LLM
        var analysisContext = BuildAnalysisContext(financialAnalysis, newsAnalysis);
        
        try
        {
            var systemPrompt = @"You are a senior investment analyst creating a comprehensive investment research report.

Based on the provided financial data and news analysis, generate a clear, actionable investment thesis.

Respond in JSON format with this exact structure:
{
    ""thesis"": ""A 2-3 sentence investment thesis explaining the key reasoning"",
    ""signal"": ""BULLISH"" | ""BEARISH"" | ""NEUTRAL"",
    ""confidence"": 0.0-1.0,
    ""insights"": [
        {""category"": ""financial"", ""insight"": ""..."", ""importance"": ""high""|""medium""|""low""},
        {""category"": ""sentiment"", ""insight"": ""..."", ""importance"": ""high""|""medium""|""low""},
        {""category"": ""growth"", ""insight"": ""..."", ""importance"": ""high""|""medium""|""low""},
        {""category"": ""risk"", ""insight"": ""..."", ""importance"": ""high""|""medium""|""low""}
    ]
}

Be specific and data-driven. Reference actual numbers when available.";

            var userPrompt = $@"Create an investment assessment for {sanitizedCompanyName} ({sanitizedSymbol}).

{analysisContext}

Provide your analysis in JSON format.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            await RecordStepAsync("Generating investment thesis with LLM");

            var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            var content = response.Value.Content[0].Text;

            // Parse the response
            var result = ParseSynthesisResult(content, sanitizedCompanyName, sanitizedSymbol);
            
            // Add sources and steps
            result.Sources = BuildSourceList(financialAnalysis, newsAnalysis);
            result.AgentSteps = allSteps
                .Select(s => new AgentStepSummary
                {
                    Agent = s.AgentName,
                    Action = s.Action,
                    Timestamp = s.Timestamp
                })
                .ToList();
            
            result.GeneratedAt = DateTime.UtcNow;

            var duration = DateTime.UtcNow - startTime;
            await RecordStepAsync(
                $"Completed synthesis: {result.Signal} with {result.Confidence:P0} confidence",
                result.Thesis,
                duration: duration);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in synthesis");
            await RecordStepAsync(
                "Synthesis failed, generating fallback result",
                isSuccess: false,
                errorMessage: ex.Message);
            
            // Return a basic result based on available data
            return GenerateFallbackResult(sanitizedCompanyName, sanitizedSymbol, financialAnalysis, newsAnalysis, allSteps);
        }
    }

    private string BuildAnalysisContext(FinancialAnalysis financial, NewsAnalysis news)
    {
        var context = new System.Text.StringBuilder();

        context.AppendLine("## Financial Data");
        context.AppendLine($"- Current Price: ${financial.Data.CurrentPrice:F2}");
        context.AppendLine($"- Price Change: {financial.Data.PriceChangePercent:F2}%");
        context.AppendLine($"- Price Trend: {financial.Trend}");
        
        if (financial.Data.MarketCap.HasValue)
            context.AppendLine($"- Market Cap: {FormatLargeNumber(financial.Data.MarketCap.Value)}");
        if (financial.Data.PeRatio.HasValue)
            context.AppendLine($"- P/E Ratio: {financial.Data.PeRatio:F2}");
        if (financial.Data.Eps.HasValue)
            context.AppendLine($"- EPS: ${financial.Data.Eps:F2}");
        if (financial.Data.RevenueGrowth.HasValue)
            context.AppendLine($"- Revenue Growth: {financial.Data.RevenueGrowth * 100:F1}%");
        if (financial.Data.NetProfitMargin.HasValue)
            context.AppendLine($"- Profit Margin: {financial.Data.NetProfitMargin * 100:F1}%");
        if (financial.Data.DebtToEquity.HasValue)
            context.AppendLine($"- Debt/Equity: {financial.Data.DebtToEquity:F2}");
        if (financial.Data.AnalystRating != null)
            context.AppendLine($"- Analyst Rating: {financial.Data.AnalystRating}");
        if (financial.Data.TargetPrice.HasValue)
            context.AppendLine($"- Target Price: ${financial.Data.TargetPrice:F2}");

        context.AppendLine();
        context.AppendLine("## News & Sentiment");
        context.AppendLine($"- Overall Sentiment: {news.SentimentLabel} ({news.OverallSentiment:F2})");
        context.AppendLine($"- Articles Analyzed: {news.Articles.Count}");
        
        if (news.KeyThemes.Count > 0)
            context.AppendLine($"- Key Themes: {string.Join(", ", news.KeyThemes)}");
        
        if (!string.IsNullOrEmpty(news.Summary))
            context.AppendLine($"- Summary: {news.Summary}");
        
        if (news.KeyEvents.Count > 0)
            context.AppendLine($"- Key Events: {string.Join("; ", news.KeyEvents)}");
        
        if (!string.IsNullOrEmpty(news.PotentialImpact))
            context.AppendLine($"- Potential Impact: {news.PotentialImpact}");

        if (news.RecentHeadlines.Count > 0)
        {
            context.AppendLine();
            context.AppendLine("Recent Headlines:");
            foreach (var headline in news.RecentHeadlines.Take(5))
            {
                context.AppendLine($"- {headline}");
            }
        }

        return context.ToString();
    }

    private AnalysisResult ParseSynthesisResult(string content, string companyName, string symbol)
    {
        try
        {
            // Use safe JSON parser
            var parsed = Tools.SafeJsonParser.ParseJsonFromText<SynthesisJson>(content);
            
            if (parsed != null)
            {
                return new AnalysisResult
                {
                    Company = $"{companyName} ({symbol})",
                    Thesis = parsed.Thesis ?? "Analysis completed",
                    Signal = NormalizeSignal(parsed.Signal),
                    Confidence = Math.Clamp(parsed.Confidence, 0, 1),
                    Insights = parsed.Insights?.Select(i => new Insight
                    {
                        Category = i.Category ?? "general",
                        Content = i.Insight ?? "",
                        Importance = i.Importance ?? "medium"
                    }).ToList() ?? new List<Insight>()
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to parse synthesis JSON");
        }

        // If parsing fails, extract what we can (safely)
        var safeContent = content.Length > 500 ? content.Substring(0, 500) : content;
        return new AnalysisResult
        {
            Company = $"{companyName} ({symbol})",
            Thesis = safeContent,
            Signal = "NEUTRAL",
            Confidence = 0.5,
            Insights = new List<Insight>()
        };
    }

    private static string NormalizeSignal(string? signal)
    {
        if (string.IsNullOrEmpty(signal))
            return "NEUTRAL";

        var upper = signal.ToUpperInvariant();
        return upper switch
        {
            "BULLISH" or "BUY" or "POSITIVE" => "BULLISH",
            "BEARISH" or "SELL" or "NEGATIVE" => "BEARISH",
            _ => "NEUTRAL"
        };
    }

    private List<DataSource> BuildSourceList(FinancialAnalysis financial, NewsAnalysis news)
    {
        var sources = new List<DataSource>();

        foreach (var source in financial.DataSources)
        {
            sources.Add(new DataSource
            {
                Type = "financial",
                Source = source,
                DataPoints = financial.Metrics.Keys.ToList()
            });
        }

        foreach (var source in news.Sources)
        {
            sources.Add(new DataSource
            {
                Type = "news",
                Source = source,
                ArticleCount = news.Articles.Count(a => true) // Could track per-source
            });
        }

        return sources;
    }

    private AnalysisResult GenerateFallbackResult(
        string companyName,
        string symbol,
        FinancialAnalysis financial,
        NewsAnalysis news,
        List<AgentStep> allSteps)
    {
        // Generate a basic result from available data
        var signal = DetermineSignalFromData(financial, news);
        var confidence = CalculateConfidence(financial, news);

        var insights = new List<Insight>();
        
        if (financial.Data.PriceChangePercent.HasValue)
        {
            insights.Add(new Insight
            {
                Category = "financial",
                Content = $"Stock is {(financial.Data.PriceChangePercent > 0 ? "up" : "down")} {Math.Abs(financial.Data.PriceChangePercent.Value):F2}% with a {financial.Trend.ToLower()} trend",
                Importance = "high"
            });
        }

        insights.Add(new Insight
        {
            Category = "sentiment",
            Content = $"News sentiment is {news.SentimentLabel.ToLower()} based on {news.Articles.Count} recent articles",
            Importance = "medium"
        });

        return new AnalysisResult
        {
            Company = $"{companyName} ({symbol})",
            Thesis = $"Based on available data, {companyName} shows {financial.Trend.ToLower()} price action with {news.SentimentLabel.ToLower()} market sentiment.",
            Signal = signal,
            Confidence = confidence,
            Insights = insights,
            Sources = BuildSourceList(financial, news),
            AgentSteps = allSteps.Select(s => new AgentStepSummary
            {
                Agent = s.AgentName,
                Action = s.Action,
                Timestamp = s.Timestamp
            }).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static string DetermineSignalFromData(FinancialAnalysis financial, NewsAnalysis news)
    {
        var score = 0;

        // Price trend
        if (financial.Trend.Contains("Up"))
            score += 2;
        else if (financial.Trend.Contains("Down"))
            score -= 2;

        // Sentiment
        if (news.OverallSentiment > 0.2)
            score += 2;
        else if (news.OverallSentiment < -0.2)
            score -= 2;

        // Analyst rating
        var rating = financial.Data.AnalystRating?.ToLower();
        if (rating != null)
        {
            if (rating.Contains("buy") || rating.Contains("strong"))
                score += 1;
            else if (rating.Contains("sell") || rating.Contains("under"))
                score -= 1;
        }

        return score switch
        {
            >= 3 => "BULLISH",
            <= -3 => "BEARISH",
            _ => "NEUTRAL"
        };
    }

    private static double CalculateConfidence(FinancialAnalysis financial, NewsAnalysis news)
    {
        var confidence = 0.5;

        // More data sources = higher confidence
        confidence += financial.DataSources.Count * 0.1;
        confidence += news.Sources.Count * 0.05;
        confidence += Math.Min(news.Articles.Count * 0.01, 0.15);

        // Strong trends increase confidence
        if (financial.Trend.Contains("Strong"))
            confidence += 0.1;

        // Strong sentiment increases confidence
        if (Math.Abs(news.OverallSentiment) > 0.3)
            confidence += 0.1;

        return Math.Clamp(confidence, 0.3, 0.95);
    }

    private static string FormatLargeNumber(decimal value)
    {
        return value switch
        {
            >= 1_000_000_000_000 => $"${value / 1_000_000_000_000:F2}T",
            >= 1_000_000_000 => $"${value / 1_000_000_000:F2}B",
            >= 1_000_000 => $"${value / 1_000_000:F2}M",
            _ => $"${value:F2}"
        };
    }

    private class SynthesisJson
    {
        public string? Thesis { get; set; }
        public string? Signal { get; set; }
        public double Confidence { get; set; }
        public List<InsightJson>? Insights { get; set; }
    }

    private class InsightJson
    {
        public string? Category { get; set; }
        public string? Insight { get; set; }
        public string? Importance { get; set; }
    }
}
