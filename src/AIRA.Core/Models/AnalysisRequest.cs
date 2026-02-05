namespace AIRA.Core.Models;

/// <summary>
/// Request model for initiating a company analysis
/// </summary>
public class AnalysisRequest
{
    /// <summary>
    /// Stock ticker symbol (e.g., "AAPL", "MSFT")
    /// </summary>
    public required string CompanySymbol { get; set; }

    /// <summary>
    /// Full company name (e.g., "Apple Inc.")
    /// </summary>
    public required string CompanyName { get; set; }

    /// <summary>
    /// Depth of analysis: "quick", "standard", or "deep"
    /// </summary>
    public string AnalysisDepth { get; set; } = "standard";
}
