using System.Buffers.Text;
using System.Net.Http.Json;
using System.Text.Json;
using AIRA.Agents.Tools.Models;
using AIRA.Agents.Tools.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIRA.Agents.Tools;

public class NewsApiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://newsapi.org/v2";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// NewsAPI tool for fetching company-related news
/// </summary>
public class NewsApiTool : INewsTool
{
    private readonly HttpClient _httpClient;
    private readonly NewsApiOptions _options;
    private readonly ILogger<NewsApiTool> _logger;


public string Name => "NewsAPI";

    public NewsApiTool(
        HttpClient httpClient,
        IOptions<NewsApiOptions> options,
        ILogger<NewsApiTool> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        // Validate API key on construction
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("NewsAPI API key is not configured");
        }
        
        // Validate HTTPS URL
        if (!ValidationHelper.IsHttpsUrl(_options.BaseUrl))
        {
            throw new InvalidOperationException("NewsAPI BaseUrl must use HTTPS");
        }
        
        // Configure timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
       
    }

    public async Task<NewsSearchResult> SearchNewsAsync(
        string companyName,
        string? symbol = null,
        int maxArticles = 10,
        int daysBack = 7,
        CancellationToken cancellationToken = default)
    {
        // Validate and sanitize inputs
        var sanitizedCompanyName = ValidationHelper.SanitizeCompanyName(companyName);
        if (string.IsNullOrWhiteSpace(sanitizedCompanyName))
        {
            throw new ArgumentException("Company name cannot be empty", nameof(companyName));
        }
        
        if (!string.IsNullOrEmpty(symbol) && !ValidationHelper.IsValidSymbol(symbol))
        {
            throw new ArgumentException($"Invalid stock symbol: {symbol}", nameof(symbol));
        }
        
        maxArticles = ValidationHelper.ValidateMaxArticles(maxArticles, 100);
        daysBack = ValidationHelper.ValidateDaysBack(daysBack, 30);
        
        var result = new NewsSearchResult
        {
            Query = sanitizedCompanyName
        };

        try
        {
            // Build search query - include both company name and symbol
            var query = !string.IsNullOrEmpty(symbol) 
                ? $"{sanitizedCompanyName} OR {symbol} stock" 
                : sanitizedCompanyName;

            var fromDate = DateTime.UtcNow.AddDays(-daysBack).ToString("yyyy-MM-dd");
            
            // Use UriBuilder for safe URL construction
            var uriBuilder = new UriBuilder($"{_options.BaseUrl}/everything");
            var queryParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryParams["q"] = query;
            queryParams["from"] = fromDate;
            queryParams["sortBy"] = "relevancy";
            queryParams["pageSize"] = maxArticles.ToString();
            queryParams["apiKey"] = _options.ApiKey;
            uriBuilder.Query = queryParams.ToString();

            _logger.LogInformation($"NewsAPI search URL => {uriBuilder.ToString()}");
            _logger.LogDebug("Searching news for {CompanyName} from NewsAPI", sanitizedCompanyName);

            var response = await _httpClient.GetFromJsonAsync<JsonElement>(uriBuilder.ToString(), cancellationToken);

            if (response.TryGetProperty(propertyName: "status", out var status) && 
                status.GetString() == "ok" &&
                response.TryGetProperty("articles", out var articles))
            {
                result.TotalResults = response.TryGetProperty("totalResults", out var total) 
                    ? total.GetInt32() 
                    : 0;

                var articleCount = 0;
                const int maxArticlesLimit = 100; // Absolute maximum
                
                foreach (var article in articles.EnumerateArray())
                {
                    if (articleCount >= maxArticlesLimit) break;
                    
                    var newsArticle = ParseArticle(article);
                    if (newsArticle != null)
                    {
                        // Calculate simple sentiment based on title/description keywords
                        newsArticle.SentimentScore = CalculateSimpleSentiment(
                            newsArticle.Title, 
                            newsArticle.Description);
                        
                        result.Articles.Add(newsArticle);
                        articleCount++;
                    }
                }

                // Calculate average sentiment
                if (result.Articles.Count > 0)
                {
                    result.AverageSentiment = result.Articles
                        .Where(a => a.SentimentScore.HasValue)
                        .Average(a => a.SentimentScore!.Value);
                }
            }
            else if (response.TryGetProperty("message", out var message))
            {
                _logger.LogWarning("NewsAPI error: {Message}", message.GetString());
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP error searching news for {CompanyName}", sanitizedCompanyName);
            throw new ApiDataException(Name, "Failed to fetch news data from NewsAPI", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Request timeout searching news for {CompanyName}", sanitizedCompanyName);
            throw new ApiTimeoutException(Name, "Request to NewsAPI timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError("Invalid JSON response from NewsAPI for {CompanyName}", sanitizedCompanyName);
            throw new ApiDataException(Name, "Invalid response format from NewsAPI", ex);
        }

        return result;
    }

    private static NewsArticle? ParseArticle(JsonElement article)
    {
        try
        {
            var title = article.TryGetProperty("title", out var t) ? t.GetString() : null;
            var url = article.TryGetProperty("url", out var u) ? u.GetString() : null;

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(url))
                return null;

            var source = article.TryGetProperty("source", out var s) && 
                         s.TryGetProperty("name", out var sn) 
                ? sn.GetString() ?? "Unknown" 
                : "Unknown";

            var publishedAt = article.TryGetProperty("publishedAt", out var p) &&
                              DateTime.TryParse(p.GetString(), out var dt)
                ? dt
                : DateTime.UtcNow;

            return new NewsArticle
            {
                Title = title,
                Description = article.TryGetProperty("description", out var d) ? d.GetString() : null,
                Content = article.TryGetProperty("content", out var c) ? c.GetString() : null,
                Source = source,
                Author = article.TryGetProperty("author", out var a) ? a.GetString() : null,
                Url = url,
                PublishedAt = publishedAt
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Simple keyword-based sentiment analysis
    /// Returns a score from -1 (very negative) to 1 (very positive)
    /// </summary>
    private static double CalculateSimpleSentiment(string? title, string? description)
    {
        var text = $"{title} {description}".ToLowerInvariant();
        
        var positiveWords = new[]
        {
            "surge", "soar", "gain", "rise", "jump", "rally", "boom", "growth",
            "profit", "beat", "exceed", "record", "strong", "bullish", "upgrade",
            "buy", "outperform", "positive", "success", "breakthrough", "innovation",
            "expand", "win", "award", "best", "leading", "top", "excellent"
        };

        var negativeWords = new[]
        {
            "drop", "fall", "decline", "crash", "plunge", "loss", "miss", "fail",
            "weak", "bearish", "downgrade", "sell", "underperform", "negative",
            "concern", "risk", "warning", "lawsuit", "investigation", "scandal",
            "layoff", "cut", "worst", "trouble", "problem", "crisis", "debt"
        };

        int positiveCount = positiveWords.Count(w => text.Contains(w));
        int negativeCount = negativeWords.Count(w => text.Contains(w));
        int totalCount = positiveCount + negativeCount;

        if (totalCount == 0)
            return 0;

        return (double)(positiveCount - negativeCount) / totalCount;
    }
}
