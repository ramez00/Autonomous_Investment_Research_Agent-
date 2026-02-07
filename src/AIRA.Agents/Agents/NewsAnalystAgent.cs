using AIRA.Agents.Tools;
using AIRA.Agents.Tools.Models;
using AIRA.Agents.Tools.Exceptions;
using Microsoft.Extensions.Logging;
using AIRA.Agents.LLM;

namespace AIRA.Agents.Agents;

/// <summary>
/// Agent responsible for gathering and analyzing news sentiment
/// </summary>
public class NewsAnalystAgent : BaseResearchAgent
{
    private readonly IEnumerable<INewsTool> _newsTools;
    private readonly ILlmClient? _llmClient;

    public override string AgentName => "NewsAnalyst";

    public NewsAnalystAgent(
        IEnumerable<INewsTool> newsTools,
        ILlmClient? llmClient,
        ILogger<NewsAnalystAgent> logger) 
        : base(logger)
    {
        _newsTools = newsTools;
        _llmClient = llmClient;
    }

    /// <summary>
    /// Gathers and analyzes news for a company
    /// </summary>
    public async Task<NewsAnalysis> AnalyzeNewsAsync(
        string companyName,
        string symbol,
        ResearchPlan plan,
        CancellationToken cancellationToken = default)
    {
        // Sanitize inputs
        var sanitizedCompanyName = Tools.ValidationHelper.SanitizeInput(companyName, 200);
        var sanitizedSymbol = Tools.ValidationHelper.SanitizeInput(symbol, 10);
        
        var startTime = DateTime.UtcNow;
        var analysis = new NewsAnalysis
        {
            CompanyName = sanitizedCompanyName,
            Symbol = sanitizedSymbol
        };

        await RecordStepAsync(
            $"Starting news analysis for {sanitizedCompanyName}",
            $"Searching {_newsTools.Count()} news sources");

        // Gather news from all sources with concurrency limit
        var semaphore = new SemaphoreSlim(3); // Maximum 3 concurrent requests
        var tasks = new List<Task>();
        const int maxTotalArticles = 100; // Absolute maximum across all sources

        foreach (var tool in _newsTools)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var toolStartTime = DateTime.UtcNow;
                    
                    var newsResult = await tool.SearchNewsAsync(
                        sanitizedCompanyName,
                        sanitizedSymbol,
                        maxArticles: 15,
                        daysBack: plan.TimeframeMonths > 6 ? 30 : 14,
                        cancellationToken);

                    if (newsResult.Articles.Count > 0)
                    {
                        lock (analysis.Articles)
                        {
                            // Enforce maximum total articles limit
                            var availableSpace = maxTotalArticles - analysis.Articles.Count;
                            if (availableSpace > 0)
                            {
                                analysis.Articles.AddRange(newsResult.Articles.Take(availableSpace));
                            }
                        }
                        
                        lock (analysis.Sources)
                        {
                            if (!analysis.Sources.Contains(tool.Name))
                                analysis.Sources.Add(tool.Name);
                        }
                        
                        var toolDuration = DateTime.UtcNow - toolStartTime;
                        await RecordStepAsync(
                            $"Retrieved {newsResult.Articles.Count} articles from {tool.Name}",
                            $"Average sentiment: {newsResult.AverageSentiment:F2}",
                            duration: toolDuration);
                    }
                }
                catch (ToolException ex)
                {
                    Logger.LogWarning("Tool error getting news from {Tool}: {ErrorCode}", tool.Name, ex.ErrorCode);
                    await RecordStepAsync(
                        $"Failed to retrieve news from {tool.Name}",
                        isSuccess: false,
                        errorMessage: $"{ex.ErrorCode}: Service temporarily unavailable");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unexpected error getting news from {Tool}", tool.Name);
                    await RecordStepAsync(
                        $"Failed to retrieve news from {tool.Name}",
                        isSuccess: false,
                        errorMessage: "Unexpected error occurred");
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }
        
        await Task.WhenAll(tasks);

        // Analyze sentiment and extract key themes
        if (analysis.Articles.Count > 0)
        {
            analysis.OverallSentiment = CalculateOverallSentiment(analysis.Articles);
            analysis.SentimentLabel = GetSentimentLabel(analysis.OverallSentiment);
            analysis.KeyThemes = ExtractKeyThemes(analysis.Articles);
            analysis.RecentHeadlines = analysis.Articles
                .OrderByDescending(a => a.PublishedAt)
                .Take(5)
                .Select(a => a.Title)
                .ToList();

            // Use LLM for deeper analysis if available
            if (_llmClient != null)
            {
                await EnhanceAnalysisWithLlm(analysis, cancellationToken);
            }
        }

        var duration = DateTime.UtcNow - startTime;
        await RecordStepAsync(
            $"Completed news analysis with {analysis.Articles.Count} articles",
            $"Sentiment: {analysis.SentimentLabel} ({analysis.OverallSentiment:F2}), Key themes: {string.Join(", ", analysis.KeyThemes.Take(3))}",
            duration: duration);

        return analysis;
    }

    private double CalculateOverallSentiment(List<NewsArticle> articles)
    {
        var articlesWithSentiment = articles
            .Where(a => a.SentimentScore.HasValue)
            .ToList();

        if (articlesWithSentiment.Count == 0)
            return 0;

        // Weight more recent articles more heavily
        var now = DateTime.UtcNow;
        var weightedSum = 0.0;
        var weightSum = 0.0;

        foreach (var article in articlesWithSentiment)
        {
            var daysAgo = (now - article.PublishedAt).TotalDays;
            var weight = Math.Max(0.1, 1 - (daysAgo / 30)); // Decay over 30 days
            
            weightedSum += article.SentimentScore!.Value * weight;
            weightSum += weight;
        }

        return weightSum > 0 ? weightedSum / weightSum : 0;
    }

    private static string GetSentimentLabel(double sentiment)
    {
        return sentiment switch
        {
            > 0.3 => "Very Positive",
            > 0.1 => "Positive",
            < -0.3 => "Very Negative",
            < -0.1 => "Negative",
            _ => "Neutral"
        };
    }

    private List<string> ExtractKeyThemes(List<NewsArticle> articles)
    {
        var themes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        var keywordPatterns = new Dictionary<string, string[]>
        {
            ["Earnings"] = new[] { "earnings", "revenue", "profit", "quarterly", "fiscal" },
            ["Growth"] = new[] { "growth", "expand", "increase", "surge", "boost" },
            ["Product"] = new[] { "product", "launch", "release", "innovation", "technology" },
            ["Market"] = new[] { "market", "competition", "competitor", "industry", "sector" },
            ["Management"] = new[] { "ceo", "executive", "leadership", "management", "board" },
            ["Regulatory"] = new[] { "regulation", "compliance", "legal", "lawsuit", "investigation" },
            ["Acquisition"] = new[] { "acquisition", "merger", "buyout", "deal", "partnership" },
            ["Dividend"] = new[] { "dividend", "buyback", "shareholder", "return" },
            ["Guidance"] = new[] { "guidance", "forecast", "outlook", "expectation" }
        };

        foreach (var article in articles)
        {
            var text = $"{article.Title} {article.Description}".ToLowerInvariant();
            
            foreach (var (theme, keywords) in keywordPatterns)
            {
                if (keywords.Any(k => text.Contains(k)))
                {
                    themes.TryGetValue(theme, out var count);
                    themes[theme] = count + 1;
                }
            }
        }

        return themes
            .OrderByDescending(t => t.Value)
            .Take(5)
            .Select(t => t.Key)
            .ToList();
    }

    private async Task EnhanceAnalysisWithLlm(NewsAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_llmClient == null || analysis.Articles.Count == 0)
            return;

        try
        {
            // Sanitize headlines before including in prompt
            var headlines = string.Join("\n", analysis.Articles
                .OrderByDescending(a => a.PublishedAt)
                .Take(10)
                .Select(a => $"- {Tools.ValidationHelper.SanitizeInput(a.Title, 300)} ({a.PublishedAt:MMM dd})"));

            var systemPrompt = @"You are a financial news analyst. Analyze the following headlines and provide:
1. A brief summary of the overall news sentiment
2. Key events or developments
3. Potential impact on stock price

Respond in JSON format:
{
    ""summary"": ""brief summary"",
    ""keyEvents"": [""event1"", ""event2""],
    ""potentialImpact"": ""brief impact assessment""
}";

            var userPrompt = $"Analyze these recent headlines for {Tools.ValidationHelper.SanitizeInput(analysis.CompanyName, 200)} ({Tools.ValidationHelper.SanitizeInput(analysis.Symbol, 10)}):\n\n{headlines}";

            var content = await _llmClient.CompleteChatAsync(systemPrompt, userPrompt, cancellationToken);

            // Use safe JSON parser
            var parsed = Tools.SafeJsonParser.ParseJsonFromText<LlmNewsAnalysis>(content);
            
            if (parsed != null)
            {
                analysis.Summary = parsed.Summary;
                analysis.KeyEvents = parsed.KeyEvents ?? new List<string>();
                analysis.PotentialImpact = parsed.PotentialImpact;
            }

            await RecordStepAsync("Enhanced analysis with LLM insights");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to enhance news analysis with LLM");
        }
    }

    private class LlmNewsAnalysis
    {
        public string? Summary { get; set; }
        public List<string>? KeyEvents { get; set; }
        public string? PotentialImpact { get; set; }
    }
}

public class NewsAnalysis
{
    public required string CompanyName { get; set; }
    public required string Symbol { get; set; }
    public List<NewsArticle> Articles { get; set; } = new();
    public List<string> Sources { get; set; } = new();
    public double OverallSentiment { get; set; }
    public string SentimentLabel { get; set; } = "Unknown";
    public List<string> KeyThemes { get; set; } = new();
    public List<string> RecentHeadlines { get; set; } = new();
    public string? Summary { get; set; }
    public List<string> KeyEvents { get; set; } = new();
    public string? PotentialImpact { get; set; }
}
