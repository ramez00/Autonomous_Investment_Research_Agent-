using AIRA.Agents.Tools;
using AIRA.Agents.Tools.Models;
using AIRA.Agents.Tools.Exceptions;
using Microsoft.Extensions.Logging;

namespace AIRA.Agents.Agents;

/// <summary>
/// Agent responsible for gathering and analyzing financial data
/// </summary>
public class FinancialDataAgent : BaseResearchAgent
{
    private readonly IEnumerable<IFinancialDataTool> _tools;

    public override string AgentName => "FinancialData";

    public FinancialDataAgent(
        IEnumerable<IFinancialDataTool> tools,
        ILogger<FinancialDataAgent> logger) 
        : base(logger)
    {
        _tools = tools;
    }

    /// <summary>
    /// Gathers comprehensive financial data for a company
    /// </summary>
    public async Task<FinancialAnalysis> GatherFinancialDataAsync(
        string symbol,
        string companyName,
        ResearchPlan plan,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var analysis = new FinancialAnalysis
        {
            Symbol = symbol,
            CompanyName = companyName
        };

        await RecordStepAsync(
            $"Starting financial data collection for {symbol}",
            $"Using {_tools.Count()} data sources");

        // Gather data from all available tools with concurrency limit
        var semaphore = new SemaphoreSlim(3); // Maximum 3 concurrent requests
        var tasks = new List<Task>();

        foreach (var tool in _tools)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var toolStartTime = DateTime.UtcNow;
                    
                    // Get quote data
                    var quote = await tool.GetQuoteAsync(symbol, cancellationToken);
                    if (quote != null)
                    {
                        lock (analysis.Data)
                        {
                            MergeFinancialData(analysis.Data, quote);
                            if (!analysis.DataSources.Contains(tool.Name))
                                analysis.DataSources.Add(tool.Name);
                        }
                    }

                    // Get fundamentals
                    var fundamentals = await tool.GetFundamentalsAsync(symbol, cancellationToken);
                    if (fundamentals != null)
                    {
                        lock (analysis.Data)
                        {
                            MergeFinancialData(analysis.Data, fundamentals);
                            if (!analysis.DataSources.Contains(tool.Name))
                                analysis.DataSources.Add(tool.Name);
                        }
                    }

                    // Get historical prices
                    var prices = await tool.GetHistoricalPricesAsync(symbol, 30, cancellationToken);
                    if (prices.Count > 0)
                    {
                        lock (analysis.HistoricalPrices)
                        {
                            // Limit total historical prices to prevent memory issues
                            const int maxTotalPrices = 1000;
                            var availableSpace = maxTotalPrices - analysis.HistoricalPrices.Count;
                            if (availableSpace > 0)
                            {
                                analysis.HistoricalPrices.AddRange(prices.Take(availableSpace));
                            }
                        }
                    }

                    var toolDuration = DateTime.UtcNow - toolStartTime;
                    await RecordStepAsync(
                        $"Retrieved data from {tool.Name}",
                        $"Quote: {quote != null}, Fundamentals: {fundamentals != null}, Historical prices: {prices.Count}",
                        duration: toolDuration);
                }
                catch (ToolException ex)
                {
                    Logger.LogWarning("Tool error getting data from {Tool}: {ErrorCode}", tool.Name, ex.ErrorCode);
                    await RecordStepAsync(
                        $"Failed to retrieve data from {tool.Name}",
                        isSuccess: false,
                        errorMessage: $"{ex.ErrorCode}: Service temporarily unavailable");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unexpected error getting data from {Tool}", tool.Name);
                    await RecordStepAsync(
                        $"Failed to retrieve data from {tool.Name}",
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

        // Analyze the collected data
        analysis.Metrics = CalculateMetrics(analysis);
        analysis.Trend = DetermineTrend(analysis.HistoricalPrices);

        var duration = DateTime.UtcNow - startTime;
        await RecordStepAsync(
            $"Completed financial analysis for {symbol}",
            $"Sources: {string.Join(", ", analysis.DataSources)}, Trend: {analysis.Trend}",
            duration: duration);

        return analysis;
    }

    private void MergeFinancialData(FinancialData target, FinancialData source)
    {
        // Merge data, preferring non-null values
        target.Symbol = source.Symbol;
        target.CompanyName = source.CompanyName ?? target.CompanyName;
        
        target.CurrentPrice ??= source.CurrentPrice;
        target.PreviousClose ??= source.PreviousClose;
        target.Open ??= source.Open;
        target.DayHigh ??= source.DayHigh;
        target.DayLow ??= source.DayLow;
        target.Week52High ??= source.Week52High;
        target.Week52Low ??= source.Week52Low;
        target.PriceChange ??= source.PriceChange;
        target.PriceChangePercent ??= source.PriceChangePercent;
        target.Volume ??= source.Volume;
        target.AverageVolume ??= source.AverageVolume;
        target.MarketCap ??= source.MarketCap;
        target.PeRatio ??= source.PeRatio;
        target.ForwardPe ??= source.ForwardPe;
        target.Eps ??= source.Eps;
        target.DividendYield ??= source.DividendYield;
        target.Beta ??= source.Beta;
        target.Revenue ??= source.Revenue;
        target.RevenueGrowth ??= source.RevenueGrowth;
        target.GrossProfit ??= source.GrossProfit;
        target.GrossProfitMargin ??= source.GrossProfitMargin;
        target.NetIncome ??= source.NetIncome;
        target.NetProfitMargin ??= source.NetProfitMargin;
        target.OperatingIncome ??= source.OperatingIncome;
        target.Ebitda ??= source.Ebitda;
        target.TotalAssets ??= source.TotalAssets;
        target.TotalLiabilities ??= source.TotalLiabilities;
        target.TotalEquity ??= source.TotalEquity;
        target.TotalDebt ??= source.TotalDebt;
        target.Cash ??= source.Cash;
        target.DebtToEquity ??= source.DebtToEquity;
        target.CurrentRatio ??= source.CurrentRatio;
        target.AnalystRating ??= source.AnalystRating;
        target.TargetPrice ??= source.TargetPrice;
        target.AnalystCount ??= source.AnalystCount;

        foreach (var ds in source.DataSources)
        {
            if (!target.DataSources.Contains(ds))
                target.DataSources.Add(ds);
        }
    }

    private Dictionary<string, object> CalculateMetrics(FinancialAnalysis analysis)
    {
        var metrics = new Dictionary<string, object>();
        var data = analysis.Data;

        // Add available metrics
        if (data.MarketCap.HasValue)
            metrics["Market Cap"] = FormatLargeNumber(data.MarketCap.Value);
        
        if (data.PeRatio.HasValue)
            metrics["P/E Ratio"] = Math.Round(data.PeRatio.Value, 2);
        
        if (data.Eps.HasValue)
            metrics["EPS"] = Math.Round(data.Eps.Value, 2);
        
        if (data.DividendYield.HasValue)
            metrics["Dividend Yield"] = $"{Math.Round(data.DividendYield.Value * 100, 2)}%";
        
        if (data.Beta.HasValue)
            metrics["Beta"] = Math.Round(data.Beta.Value, 2);
        
        if (data.DebtToEquity.HasValue)
            metrics["Debt/Equity"] = Math.Round(data.DebtToEquity.Value, 2);
        
        if (data.CurrentRatio.HasValue)
            metrics["Current Ratio"] = Math.Round(data.CurrentRatio.Value, 2);
        
        if (data.NetProfitMargin.HasValue)
            metrics["Net Margin"] = $"{Math.Round(data.NetProfitMargin.Value * 100, 2)}%";
        
        if (data.RevenueGrowth.HasValue)
            metrics["Revenue Growth"] = $"{Math.Round(data.RevenueGrowth.Value * 100, 2)}%";

        // Calculate price position in 52-week range
        if (data.CurrentPrice.HasValue && data.Week52High.HasValue && data.Week52Low.HasValue)
        {
            var range = data.Week52High.Value - data.Week52Low.Value;
            if (range > 0)
            {
                var position = (data.CurrentPrice.Value - data.Week52Low.Value) / range * 100;
                metrics["52W Range Position"] = $"{Math.Round(position, 1)}%";
            }
        }

        return metrics;
    }

    private string DetermineTrend(List<HistoricalPrice> prices)
    {
        if (prices.Count < 5)
            return "Insufficient Data";

        // Calculate simple trend based on price movement
        var recentPrices = prices.OrderByDescending(p => p.Date).Take(5).ToList();
        var olderPrices = prices.OrderByDescending(p => p.Date).Skip(5).Take(5).ToList();

        if (olderPrices.Count == 0)
            return "Neutral";

        var recentAvg = recentPrices.Average(p => p.Close);
        var olderAvg = olderPrices.Average(p => p.Close);

        var change = (recentAvg - olderAvg) / olderAvg * 100;

        return change switch
        {
            > 5 => "Strong Uptrend",
            > 2 => "Uptrend",
            < -5 => "Strong Downtrend",
            < -2 => "Downtrend",
            _ => "Neutral"
        };
    }

    private static string FormatLargeNumber(decimal value)
    {
        return value switch
        {
            >= 1_000_000_000_000 => $"${value / 1_000_000_000_000:F2}T",
            >= 1_000_000_000 => $"${value / 1_000_000_000:F2}B",
            >= 1_000_000 => $"${value / 1_000_000:F2}M",
            >= 1_000 => $"${value / 1_000:F2}K",
            _ => $"${value:F2}"
        };
    }
}

public class FinancialAnalysis
{
    public required string Symbol { get; set; }
    public required string CompanyName { get; set; }
    public FinancialData Data { get; set; } = new() { Symbol = "", CompanyName = "" };
    public List<HistoricalPrice> HistoricalPrices { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public string Trend { get; set; } = "Unknown";
    public List<string> DataSources { get; set; } = new();
}
