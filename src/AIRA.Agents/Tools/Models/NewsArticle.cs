namespace AIRA.Agents.Tools.Models;

/// <summary>
/// News article from news APIs
/// </summary>
public class NewsArticle
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public required string Source { get; set; }
    public string? Author { get; set; }
    public required string Url { get; set; }
    public DateTime PublishedAt { get; set; }
    
    /// <summary>
    /// Sentiment score from -1 (very negative) to 1 (very positive)
    /// </summary>
    public double? SentimentScore { get; set; }
    
    /// <summary>
    /// Relevance score from 0 to 1
    /// </summary>
    public double? RelevanceScore { get; set; }
}

public class NewsSearchResult
{
    public required string Query { get; set; }
    public int TotalResults { get; set; }
    public List<NewsArticle> Articles { get; set; } = new();
    public double AverageSentiment { get; set; }
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}
