using System.Net.Http.Json;
using System.Text.Json;
using AIRA.Agents.Tools.Models;
using AIRA.Agents.Tools.Exceptions;
using Microsoft.Extensions.Logging;

namespace AIRA.Agents.Tools;

/// <summary>
/// Yahoo Finance API tool for stock data
/// Uses the unofficial Yahoo Finance API endpoints
/// </summary>
public class YahooFinanceTool : IFinancialDataTool
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YahooFinanceTool> _logger;
    private const string BaseUrl = "https://query1.finance.yahoo.com/v8/finance";

    public string Name => "YahooFinance";

    public YahooFinanceTool(HttpClient httpClient, ILogger<YahooFinanceTool> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Validate BaseUrl is HTTPS
        if (!ValidationHelper.IsHttpsUrl(BaseUrl))
        {
            throw new InvalidOperationException("Yahoo Finance BaseUrl must use HTTPS");
        }
        
        // Configure timeout (30 seconds default)
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // Set required headers for Yahoo Finance API
        // Note: User-Agent spoofing is necessary for Yahoo Finance to work
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<FinancialData?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (!ValidationHelper.IsValidSymbol(symbol))
        {
            throw new ArgumentException($"Invalid stock symbol: {symbol}", nameof(symbol));
        }
        
        try
        {
            var url = $"{BaseUrl}/chart/{symbol}";

            _logger.LogInformation($"YahooFinance Url => {url}");
            
            _logger.LogDebug("Fetching quote from Yahoo Finance for {Symbol}", symbol);
            
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
            
            if (response.TryGetProperty("chart", out var chart) &&
                chart.TryGetProperty("result", out var results) &&
                results.GetArrayLength() > 0)
            {
                var result = results[0];
                var meta = result.GetProperty("meta");
                
                var data = new FinancialData
                {
                    Symbol = symbol,
                    CompanyName = GetStringOrDefault(meta, "shortName") ?? 
                                  GetStringOrDefault(meta, "longName") ?? symbol,
                    CurrentPrice = GetDecimalOrNull(meta, "regularMarketPrice"),
                    PreviousClose = GetDecimalOrNull(meta, "previousClose") ?? 
                                    GetDecimalOrNull(meta, "chartPreviousClose"),
                    DayHigh = GetDecimalOrNull(meta, "regularMarketDayHigh"),
                    DayLow = GetDecimalOrNull(meta, "regularMarketDayLow"),
                    Volume = GetLongOrNull(meta, "regularMarketVolume"),
                    Week52High = GetDecimalOrNull(meta, "fiftyTwoWeekHigh"),
                    Week52Low = GetDecimalOrNull(meta, "fiftyTwoWeekLow")
                };
                
                // Calculate price change
                if (data.CurrentPrice.HasValue && data.PreviousClose.HasValue)
                {
                    data.PriceChange = data.CurrentPrice.Value - data.PreviousClose.Value;
                    data.PriceChangePercent = data.PreviousClose.Value != 0 
                        ? (data.PriceChange.Value / data.PreviousClose.Value) * 100 
                        : 0;
                }
                
                data.DataSources.Add(Name);
                return data;
            }
            
            _logger.LogWarning("No quote data returned from Yahoo Finance for {Symbol}", symbol);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP error fetching quote from Yahoo Finance for {Symbol}", symbol);
            throw new ApiDataException(Name, "Failed to fetch quote data from Yahoo Finance", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Request timeout fetching quote from Yahoo Finance for {Symbol}", symbol);
            throw new ApiTimeoutException(Name, "Request to Yahoo Finance timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError("Invalid JSON response from Yahoo Finance for {Symbol}", symbol);
            throw new ApiDataException(Name, "Invalid response format from Yahoo Finance", ex);
        }
    }

    public async Task<FinancialData?> GetFundamentalsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (!ValidationHelper.IsValidSymbol(symbol))
        {
            throw new ArgumentException($"Invalid stock symbol: {symbol}", nameof(symbol));
        }
        
        try
        {
            // Use the quoteSummary endpoint for detailed fundamentals
            var url = $"https://query1.finance.yahoo.com/v10/finance/quoteSummary/{symbol}?modules=summaryDetail,financialData,defaultKeyStatistics,earnings";
            
            _logger.LogInformation($"YahooFinance Url => {url}");

            _logger.LogDebug("Fetching fundamentals from Yahoo Finance for {Symbol}", symbol);
            
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
            
            if (response.TryGetProperty("quoteSummary", out var summary) &&
                summary.TryGetProperty("result", out var results) &&
                results.GetArrayLength() > 0)
            {
                var result = results[0];
                var summaryDetail = GetPropertyOrNull(result, "summaryDetail");
                var financialData = GetPropertyOrNull(result, "financialData");
                var keyStats = GetPropertyOrNull(result, "defaultKeyStatistics");
                
                var data = new FinancialData
                {
                    Symbol = symbol,
                    CompanyName = symbol,
                    MarketCap = GetRawValue(summaryDetail, "marketCap"),
                    PeRatio = GetRawValue(summaryDetail, "trailingPE"),
                    ForwardPe = GetRawValue(summaryDetail, "forwardPE"),
                    DividendYield = GetRawValue(summaryDetail, "dividendYield"),
                    Beta = GetRawValue(summaryDetail, "beta"),
                    Week52High = GetRawValue(summaryDetail, "fiftyTwoWeekHigh"),
                    Week52Low = GetRawValue(summaryDetail, "fiftyTwoWeekLow"),
                    AverageVolume = (long?)GetRawValue(summaryDetail, "averageVolume"),
                    Revenue = GetRawValue(financialData, "totalRevenue"),
                    RevenueGrowth = GetRawValue(financialData, "revenueGrowth"),
                    GrossProfitMargin = GetRawValue(financialData, "grossMargins"),
                    NetProfitMargin = GetRawValue(financialData, "profitMargins"),
                    Ebitda = GetRawValue(financialData, "ebitda"),
                    TotalDebt = GetRawValue(financialData, "totalDebt"),
                    TotalEquity = GetRawValue(keyStats, "bookValue"),
                    DebtToEquity = GetRawValue(financialData, "debtToEquity"),
                    CurrentRatio = GetRawValue(financialData, "currentRatio"),
                    AnalystRating = GetFormattedValue(financialData, "recommendationKey"),
                    TargetPrice = GetRawValue(financialData, "targetMeanPrice"),
                    AnalystCount = (int?)GetRawValue(financialData, "numberOfAnalystOpinions"),
                    Eps = GetRawValue(keyStats, "trailingEps")
                };
                
                data.DataSources.Add(Name);
                return data;
            }
            
            _logger.LogWarning("No fundamentals data returned from Yahoo Finance for {Symbol}", symbol);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP error fetching fundamentals from Yahoo Finance for {Symbol}", symbol);
            throw new ApiDataException(Name, "Failed to fetch fundamentals data from Yahoo Finance", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Request timeout fetching fundamentals from Yahoo Finance for {Symbol}", symbol);
            throw new ApiTimeoutException(Name, "Request to Yahoo Finance timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError("Invalid JSON response from Yahoo Finance for {Symbol}", symbol);
            throw new ApiDataException(Name, "Invalid response format from Yahoo Finance", ex);
        }
    }

    public async Task<List<HistoricalPrice>> GetHistoricalPricesAsync(
        string symbol,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!ValidationHelper.IsValidSymbol(symbol))
        {
            throw new ArgumentException($"Invalid stock symbol: {symbol}", nameof(symbol));
        }
        
        days = ValidationHelper.ValidateDaysBack(days, 365);
        var prices = new List<HistoricalPrice>();
        
        try
        {
            var range = days <= 5 ? "5d" : days <= 30 ? "1mo" : days <= 90 ? "3mo" : "6mo";
            var url = $"{BaseUrl}/chart/{symbol}?interval=1d&range={range}";
            
            _logger.LogDebug("Fetching historical prices from Yahoo Finance for {Symbol}", symbol);
            
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
            
            if (response.TryGetProperty("chart", out var chart) &&
                chart.TryGetProperty("result", out var results) &&
                results.GetArrayLength() > 0)
            {
                var result = results[0];
                
                if (result.TryGetProperty("timestamp", out var timestamps) &&
                    result.TryGetProperty("indicators", out var indicators) &&
                    indicators.TryGetProperty("quote", out var quotes) &&
                    quotes.GetArrayLength() > 0)
                {
                    var quote = quotes[0];
                    var opens = GetArrayOrEmpty(quote, "open");
                    var highs = GetArrayOrEmpty(quote, "high");
                    var lows = GetArrayOrEmpty(quote, "low");
                    var closes = GetArrayOrEmpty(quote, "close");
                    var volumes = GetArrayOrEmpty(quote, "volume");
                    
                    var adjClose = indicators.TryGetProperty("adjclose", out var adjCloseArr) &&
                                   adjCloseArr.GetArrayLength() > 0
                        ? GetArrayOrEmpty(adjCloseArr[0], "adjclose")
                        : closes;
                    
                    const int maxPrices = 1000; // Absolute maximum to prevent memory issues
                    var safeDays = Math.Min(days, maxPrices);
                    var maxItems = Math.Min(timestamps.GetArrayLength(), safeDays);
                    
                    for (int i = 0; i < maxItems; i++)
                    {
                        var ts = timestamps[i].GetInt64();
                        var date = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime;
                        
                        prices.Add(new HistoricalPrice
                        {
                            Date = date,
                            Open = GetDecimalAtIndex(opens, i),
                            High = GetDecimalAtIndex(highs, i),
                            Low = GetDecimalAtIndex(lows, i),
                            Close = GetDecimalAtIndex(closes, i),
                            AdjustedClose = GetDecimalAtIndex(adjClose, i),
                            Volume = GetLongAtIndex(volumes, i)
                        });
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP error fetching historical prices from Yahoo Finance for {Symbol}", symbol);
            throw new ApiDataException(Name, "Failed to fetch historical prices from Yahoo Finance", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Request timeout fetching historical prices from Yahoo Finance for {Symbol}", symbol);
            throw new ApiTimeoutException(Name, "Request to Yahoo Finance timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError("Invalid JSON response from Yahoo Finance for {Symbol}", symbol);
            throw new ApiDataException(Name, "Invalid response format from Yahoo Finance", ex);
        }
        
        return prices.OrderByDescending(p => p.Date).ToList();
    }

    private static string? GetStringOrDefault(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString();
        return null;
    }

    private static decimal? GetDecimalOrNull(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number)
                return value.GetDecimal();
        }
        return null;
    }

    private static long? GetLongOrNull(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number)
                return value.GetInt64();
        }
        return null;
    }

    private static JsonElement? GetPropertyOrNull(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
            return value;
        return null;
    }

    private static decimal? GetRawValue(JsonElement? element, string property)
    {
        if (element == null) return null;
        
        if (element.Value.TryGetProperty(property, out var prop) &&
            prop.TryGetProperty("raw", out var raw) &&
            raw.ValueKind == JsonValueKind.Number)
        {
            return raw.GetDecimal();
        }
        return null;
    }

    private static string? GetFormattedValue(JsonElement? element, string property)
    {
        if (element == null) return null;
        
        if (element.Value.TryGetProperty(property, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static JsonElement GetArrayOrEmpty(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var arr))
            return arr;
        return default;
    }

    private static decimal GetDecimalAtIndex(JsonElement array, int index)
    {
        if (array.ValueKind == JsonValueKind.Array && index < array.GetArrayLength())
        {
            var item = array[index];
            if (item.ValueKind == JsonValueKind.Number)
                return item.GetDecimal();
        }
        return 0;
    }

    private static long GetLongAtIndex(JsonElement array, int index)
    {
        if (array.ValueKind == JsonValueKind.Array && index < array.GetArrayLength())
        {
            var item = array[index];
            if (item.ValueKind == JsonValueKind.Number)
                return item.GetInt64();
        }
        return 0;
    }
}
