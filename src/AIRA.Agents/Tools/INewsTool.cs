using AIRA.Agents.Tools.Models;

namespace AIRA.Agents.Tools;

/// <summary>
/// Interface for news retrieval tools
/// </summary>
public interface INewsTool
{
    string Name { get; }
    
    /// <summary>
    /// Search for news articles about a company
    /// </summary>
    Task<NewsSearchResult> SearchNewsAsync(
        string companyName,
        string? symbol = null,
        int maxArticles = 10,
        int daysBack = 7,
        CancellationToken cancellationToken = default);
}
