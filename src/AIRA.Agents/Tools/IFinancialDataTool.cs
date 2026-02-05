using AIRA.Agents.Tools.Models;

namespace AIRA.Agents.Tools;

/// <summary>
/// Interface for financial data retrieval tools
/// </summary>
public interface IFinancialDataTool
{
    string Name { get; }
    
    /// <summary>
    /// Get current quote and basic company info
    /// </summary>
    Task<FinancialData?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get company fundamentals (income statement, balance sheet, etc.)
    /// </summary>
    Task<FinancialData?> GetFundamentalsAsync(string symbol, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get historical price data
    /// </summary>
    Task<List<HistoricalPrice>> GetHistoricalPricesAsync(
        string symbol,
        int days = 30,
        CancellationToken cancellationToken = default);
}
