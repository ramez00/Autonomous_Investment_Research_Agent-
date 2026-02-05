using System.Text.Json;

namespace AIRA.Agents.Tools;

/// <summary>
/// Safe JSON parsing utilities with validation
/// </summary>
public static class SafeJsonParser
{
    private const int MaxJsonDepth = 64;
    private const int MaxJsonLength = 1_000_000; // 1MB
    
    /// <summary>
    /// Safely extracts and parses JSON from text content
    /// </summary>
    public static T? ParseJsonFromText<T>(string content, JsonSerializerOptions? options = null) where T : class
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;
            
        if (content.Length > MaxJsonLength)
        {
            throw new InvalidOperationException($"JSON content exceeds maximum length of {MaxJsonLength} characters");
        }
        
        try
        {
            // Find JSON boundaries safely
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            
            if (jsonStart < 0 || jsonEnd <= jsonStart)
                return null;
            
            // Ensure we don't exceed bounds
            if (jsonEnd >= content.Length)
                return null;
                
            var jsonLength = jsonEnd - jsonStart + 1;
            if (jsonLength > MaxJsonLength)
            {
                throw new InvalidOperationException($"Extracted JSON exceeds maximum length of {MaxJsonLength} characters");
            }
            
            var json = content.Substring(jsonStart, jsonLength);
            
            // Configure safe deserialization options
            var safeOptions = options ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                MaxDepth = MaxJsonDepth,
                AllowTrailingCommas = true
            };
            
            return JsonSerializer.Deserialize<T>(json, safeOptions);
        }
        catch (JsonException)
        {
            // Invalid JSON format
            return null;
        }
        catch (ArgumentOutOfRangeException)
        {
            // Substring operation failed
            return null;
        }
    }
    
    /// <summary>
    /// Safely deserializes JSON with depth and size limits
    /// </summary>
    public static T? SafeDeserialize<T>(string json, JsonSerializerOptions? options = null) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
            
        if (json.Length > MaxJsonLength)
        {
            throw new InvalidOperationException($"JSON content exceeds maximum length of {MaxJsonLength} characters");
        }
        
        try
        {
            var safeOptions = options ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                MaxDepth = MaxJsonDepth,
                AllowTrailingCommas = true
            };
            
            return JsonSerializer.Deserialize<T>(json, safeOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
    
    /// <summary>
    /// Validates that a string contains valid JSON
    /// </summary>
    public static bool IsValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;
            
        if (json.Length > MaxJsonLength)
            return false;
        
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                MaxDepth = MaxJsonDepth,
                AllowTrailingCommas = true
            });
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
