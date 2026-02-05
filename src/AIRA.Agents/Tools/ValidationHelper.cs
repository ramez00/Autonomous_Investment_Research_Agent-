using System.Text.RegularExpressions;

namespace AIRA.Agents.Tools;

/// <summary>
/// Helper class for input validation to prevent injection attacks
/// </summary>
public static partial class ValidationHelper
{
    private const int MaxSymbolLength = 10;
    private const int MaxCompanyNameLength = 200;
    private const int MaxInputLength = 500;
    
    [GeneratedRegex("^[A-Z0-9.-]{1,10}$", RegexOptions.Compiled)]
    private static partial Regex SymbolRegex();
    
    /// <summary>
    /// Validates a stock symbol
    /// </summary>
    public static bool IsValidSymbol(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return false;
            
        if (symbol.Length > MaxSymbolLength)
            return false;
            
        return SymbolRegex().IsMatch(symbol.ToUpperInvariant());
    }
    
    /// <summary>
    /// Validates and sanitizes a company name
    /// </summary>
    public static string? SanitizeCompanyName(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return null;
            
        // Remove control characters and limit length
        var sanitized = RemoveControlCharacters(companyName);
        
        if (sanitized.Length > MaxCompanyNameLength)
            sanitized = sanitized.Substring(0, MaxCompanyNameLength);
            
        return sanitized;
    }
    
    /// <summary>
    /// Sanitizes general input for LLM prompts
    /// </summary>
    public static string SanitizeInput(string? input, int maxLength = MaxInputLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
            
        // Remove control characters and potentially malicious sequences
        var sanitized = RemoveControlCharacters(input);
        
        // Remove potential prompt injection patterns
        sanitized = RemovePromptInjectionPatterns(sanitized);
        
        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);
            
        return sanitized;
    }
    
    /// <summary>
    /// Validates numeric values are within reasonable ranges
    /// </summary>
    public static bool IsValidPrice(decimal? price)
    {
        if (!price.HasValue)
            return true; // Null is acceptable
            
        return price.Value >= 0 && price.Value <= 1_000_000_000; // Max $1B per share
    }
    
    /// <summary>
    /// Validates days back parameter
    /// </summary>
    public static int ValidateDaysBack(int daysBack, int maxDays = 365)
    {
        if (daysBack < 1)
            return 1;
        if (daysBack > maxDays)
            return maxDays;
        return daysBack;
    }
    
    /// <summary>
    /// Validates max articles parameter
    /// </summary>
    public static int ValidateMaxArticles(int maxArticles, int maxAllowed = 100)
    {
        if (maxArticles < 1)
            return 1;
        if (maxArticles > maxAllowed)
            return maxAllowed;
        return maxArticles;
    }
    
    private static string RemoveControlCharacters(string input)
    {
        return new string(input.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t').ToArray());
    }
    
    private static string RemovePromptInjectionPatterns(string input)
    {
        // Remove common prompt injection attempts
        var patterns = new[]
        {
            "ignore previous instructions",
            "ignore all previous",
            "disregard previous",
            "forget previous",
            "new instructions:",
            "system:",
            "assistant:",
            "user:",
            "[INST]",
            "[/INST]",
            "<|im_start|>",
            "<|im_end|>"
        };
        
        var result = input;
        foreach (var pattern in patterns)
        {
            result = result.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
        }
        
        return result;
    }
    
    /// <summary>
    /// Validates a URL is HTTPS
    /// </summary>
    public static bool IsHttpsUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
            
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               uri.Scheme == Uri.UriSchemeHttps;
    }
}
