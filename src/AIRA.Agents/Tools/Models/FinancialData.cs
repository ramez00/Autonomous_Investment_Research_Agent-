namespace AIRA.Agents.Tools.Models;

/// <summary>
/// Financial data retrieved from APIs
/// </summary>
public class FinancialData
{
    public required string Symbol { get; set; }
    public required string CompanyName { get; set; }
    
    // Price data
    public decimal? CurrentPrice { get; set; }
    public decimal? PreviousClose { get; set; }
    public decimal? Open { get; set; }
    public decimal? DayHigh { get; set; }
    public decimal? DayLow { get; set; }
    public decimal? Week52High { get; set; }
    public decimal? Week52Low { get; set; }
    public decimal? PriceChange { get; set; }
    public decimal? PriceChangePercent { get; set; }
    
    // Volume
    public long? Volume { get; set; }
    public long? AverageVolume { get; set; }
    
    // Fundamentals
    public decimal? MarketCap { get; set; }
    public decimal? PeRatio { get; set; }
    public decimal? ForwardPe { get; set; }
    public decimal? Eps { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? Beta { get; set; }
    
    // Financials
    public decimal? Revenue { get; set; }
    public decimal? RevenueGrowth { get; set; }
    public decimal? GrossProfit { get; set; }
    public decimal? GrossProfitMargin { get; set; }
    public decimal? NetIncome { get; set; }
    public decimal? NetProfitMargin { get; set; }
    public decimal? OperatingIncome { get; set; }
    public decimal? Ebitda { get; set; }
    
    // Balance sheet
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? TotalEquity { get; set; }
    public decimal? TotalDebt { get; set; }
    public decimal? Cash { get; set; }
    public decimal? DebtToEquity { get; set; }
    public decimal? CurrentRatio { get; set; }
    
    // Analyst data
    public string? AnalystRating { get; set; }
    public decimal? TargetPrice { get; set; }
    public int? AnalystCount { get; set; }
    
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    public List<string> DataSources { get; set; } = new();
}

public class HistoricalPrice
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal AdjustedClose { get; set; }
    public long Volume { get; set; }
}
