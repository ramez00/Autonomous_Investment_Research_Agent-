using System.Net.Http.Json;
using System.Text.Json;
using AIRA.Agents.Tools.Models;
using AIRA.Agents.Tools.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIRA.Agents.Tools;

public class AlphaVantageOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.alphavantage.co/query";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Alpha Vantage API tool for financial data
/// </summary>
public class AlphaVantageTool : IFinancialDataTool
{
    private readonly HttpClient _httpClient;
    private readonly AlphaVantageOptions _options;
    private readonly ILogger<AlphaVantageTool> _logger;

    public string Name => "AlphaVantage";

    public AlphaVantageTool(
        HttpClient httpClient,
        IOptions<AlphaVantageOptions> options,
        ILogger<AlphaVantageTool> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        // Validate API key on construction
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("AlphaVantage API key is not configured");
        }
        
        // Validate HTTPS URL
        if (!ValidationHelper.IsHttpsUrl(_options.BaseUrl))
        {
            throw new InvalidOperationException("AlphaVantage BaseUrl must use HTTPS");
        }
        
        // Configure timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<FinancialData?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (!ValidationHelper.IsValidSymbol(symbol))
        {
            throw new ArgumentException($"Invalid stock symbol: {symbol}", nameof(symbol));
        }
        
        try
        {
            var url = $"{_options.BaseUrl}?function=GLOBAL_QUOTE&symbol={symbol}";
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);

            _logger.LogDebug("Fetching quote from Alpha Vantage for {Symbol}", symbol);
            
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
            
            if (response.TryGetProperty("Global Quote", out var quote))
            {
                var data = new FinancialData
                {
                    Symbol = symbol,
                    CompanyName = symbol, // Alpha Vantage doesn't return company name in quote
                    CurrentPrice = ParseDecimal(quote, "05. price"),
                    PreviousClose = ParseDecimal(quote, "08. previous close"),
                    Open = ParseDecimal(quote, "02. open"),
                    DayHigh = ParseDecimal(quote, "03. high"),
                    DayLow = ParseDecimal(quote, "04. low"),
                    Volume = ParseLong(quote, "06. volume"),
                    PriceChange = ParseDecimal(quote, "09. change"),
                    PriceChangePercent = ParsePercentage(quote, "10. change percent")
                };
                
                data.DataSources.Add(Name);
                return data;
            }
            
            _logger.LogWarning("No quote data returned from Alpha Vantage for {Symbol}", symbol);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP error fetching quote from Alpha Vantage for {Symbol}", symbol);
            throw new ApiDataException(Name, "Failed to fetch quote data from Alpha Vantage", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Request timeout fetching quote from Alpha Vantage for {Symbol}", symbol);
            throw new ApiTimeoutException(Name, "Request to Alpha Vantage timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError("Invalid JSON response from Alpha Vantage for {Symbol}", symbol);
            throw new ApiDataException(Name, "Invalid response format from Alpha Vantage", ex);
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
            var url = $"{_options.BaseUrl}?function=OVERVIEW&symbol={symbol}";
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);

            _logger.LogDebug("Fetching fundamentals from Alpha Vantage for {Symbol}", symbol);
            
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
            
            if (response.ValueKind == JsonValueKind.Object && response.TryGetProperty("Symbol", out _))
            {
                var data = new FinancialData
                {
                    Symbol = symbol,
                    CompanyName = GetStringValue(response, "Name") ?? symbol,
                    MarketCap = ParseDecimal(response, "MarketCapitalization"),
                    PeRatio = ParseDecimal(response, "PERatio"),
                    ForwardPe = ParseDecimal(response, "ForwardPE"),
                    Eps = ParseDecimal(response, "EPS"),
                    DividendYield = ParseDecimal(response, "DividendYield"),
                    Beta = ParseDecimal(response, "Beta"),
                    Week52High = ParseDecimal(response, "52WeekHigh"),
                    Week52Low = ParseDecimal(response, "52WeekLow"),
                    Revenue = ParseDecimal(response, "RevenueTTM"),
                    GrossProfitMargin = ParseDecimal(response, "GrossProfitTTM"),
                    NetProfitMargin = ParseDecimal(response, "ProfitMargin"),
                    OperatingIncome = ParseDecimal(response, "OperatingMarginTTM"),
                    Ebitda = ParseDecimal(response, "EBITDA"),
                    TotalAssets = ParseDecimal(response, "TotalAssets") ?? ParseDecimal(response, "BookValue"),
                    AnalystRating = GetStringValue(response, "AnalystRating"),
                    TargetPrice = ParseDecimal(response, "AnalystTargetPrice")
                };
                
                data.DataSources.Add(Name);
                return data;
            }
            
            _logger.LogWarning("No fundamentals data returned from Alpha Vantage for {Symbol}", symbol);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP error fetching fundamentals from Alpha Vantage for {Symbol}", symbol);
            throw new ApiDataException(Name, "Failed to fetch fundamentals data from Alpha Vantage", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Request timeout fetching fundamentals from Alpha Vantage for {Symbol}", symbol);
            throw new ApiTimeoutException(Name, "Request to Alpha Vantage timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError("Invalid JSON response from Alpha Vantage for {Symbol}", symbol);
            throw new ApiDataException(Name, "Invalid response format from Alpha Vantage", ex);
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
            var url = $"{_options.BaseUrl}?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize=compact";
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);

            _logger.LogDebug("Fetching historical prices from Alpha Vantage for {Symbol}", symbol);
            
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
            
            if (response.TryGetProperty("Time Series (Daily)", out var timeSeries))
            {
                var count = 0;
                const int maxPrices = 1000; // Absolute maximum to prevent memory issues
                var safeDays = Math.Min(days, maxPrices);
                
                foreach (var day in timeSeries.EnumerateObject())
                {
                    if (count >= safeDays) break;
                    
                    if (DateTime.TryParse(day.Name, out var date))
                    {
                        prices.Add(new HistoricalPrice
                        {
                            Date = date,
                            Open = ParseDecimal(day.Value, "1. open") ?? 0,
                            High = ParseDecimal(day.Value, "2. high") ?? 0,
                            Low = ParseDecimal(day.Value, "3. low") ?? 0,
                            Close = ParseDecimal(day.Value, "4. close") ?? 0,
                            AdjustedClose = ParseDecimal(day.Value, "5. adjusted close") ?? ParseDecimal(day.Value, "4. close") ?? 0,
                            Volume = ParseLong(day.Value, "5. volume") ?? ParseLong(day.Value, "6. volume") ?? 0
                        });
                    }
                    count++;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP error fetching historical prices from Alpha Vantage for {Symbol}", symbol);
            throw new ApiDataException(Name, "Failed to fetch historical prices from Alpha Vantage", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("Request timeout fetching historical prices from Alpha Vantage for {Symbol}", symbol);
            throw new ApiTimeoutException(Name, "Request to Alpha Vantage timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError("Invalid JSON response from Alpha Vantage for {Symbol}", symbol);
            throw new ApiDataException(Name, "Invalid response format from Alpha Vantage", ex);
        }
        
        return prices;
    }

    private static decimal? ParseDecimal(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
        {
            var str = value.GetString();
            if (!string.IsNullOrEmpty(str) && str != "None" && str != "-")
            {
                if (decimal.TryParse(str, out var result))
                    return result;
            }
        }
        return null;
    }

    private static decimal? ParsePercentage(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
        {
            var str = value.GetString()?.TrimEnd('%');
            if (!string.IsNullOrEmpty(str) && decimal.TryParse(str, out var result))
                return result;
        }
        return null;
    }

    private static long? ParseLong(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
        {
            var str = value.GetString();
            if (!string.IsNullOrEmpty(str) && long.TryParse(str, out var result))
                return result;
        }
        return null;
    }

    private static string? GetStringValue(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value))
        {
            var str = value.GetString();
            if (!string.IsNullOrEmpty(str) && str != "None" && str != "-")
                return str;
        }
        return null;
    }
}
