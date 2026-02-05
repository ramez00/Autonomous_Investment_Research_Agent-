# Security Quick Reference Guide

Quick reference for developers working with the AIRA.Agents codebase.

## ✅ Do's

### Input Validation
```csharp
// ✅ Always validate stock symbols
if (!ValidationHelper.IsValidSymbol(symbol))
    throw new ArgumentException($"Invalid symbol: {symbol}", nameof(symbol));

// ✅ Always sanitize company names
var sanitized = ValidationHelper.SanitizeCompanyName(companyName);

// ✅ Sanitize inputs before LLM prompts
var safe = ValidationHelper.SanitizeInput(userInput, maxLength: 200);
```

### URL Construction
```csharp
// ✅ Use UriBuilder for URL construction
var uriBuilder = new UriBuilder(baseUrl);
var queryParams = HttpUtility.ParseQueryString(string.Empty);
queryParams["q"] = userInput;
uriBuilder.Query = queryParams.ToString();
```

### JSON Parsing
```csharp
// ✅ Use SafeJsonParser for LLM responses
var parsed = SafeJsonParser.ParseJsonFromText<MyType>(llmResponse);
```

### Error Handling
```csharp
// ✅ Use specific exception types
try
{
    await apiCall();
}
catch (ApiTimeoutException ex)
{
    // Handle timeout
}
catch (ApiDataException ex)
{
    // Handle data errors
}
catch (ToolException ex)
{
    // Handle other tool errors
}
```

### Secrets Management
```csharp
// ✅ Development: Use User Secrets
dotnet user-secrets set "ApiKey" "value"

// ✅ Production: Use Azure Key Vault
builder.Configuration.AddAzureKeyVault(vaultUri, credentials);

// ✅ Always validate configuration
if (string.IsNullOrWhiteSpace(options.ApiKey))
    throw new InvalidOperationException("API key not configured");
```

### HTTP Clients
```csharp
// ✅ Validate HTTPS
if (!ValidationHelper.IsHttpsUrl(baseUrl))
    throw new InvalidOperationException("Must use HTTPS");

// ✅ Set timeouts
_httpClient.Timeout = TimeSpan.FromSeconds(30);
```

### Concurrency
```csharp
// ✅ Use SemaphoreSlim for concurrency limits
var semaphore = new SemaphoreSlim(3);
await semaphore.WaitAsync(cancellationToken);
try
{
    await DoWork();
}
finally
{
    semaphore.Release();
}
```

### Resource Limits
```csharp
// ✅ Enforce collection size limits
const int maxItems = 1000;
if (collection.Count >= maxItems) break;

// ✅ Validate numeric ranges
days = ValidationHelper.ValidateDaysBack(days, maxDays: 365);
```

---

## ❌ Don'ts

### Input Handling
```csharp
// ❌ Never use raw user input in URLs
var url = $"{baseUrl}?q={userInput}"; // UNSAFE!

// ❌ Never use raw input in LLM prompts
var prompt = $"Analyze {companyName}"; // UNSAFE!

// ❌ Never trust API responses without validation
var symbol = response["symbol"]; // UNSAFE!
```

### URL Construction
```csharp
// ❌ Never manually concatenate URLs
var url = baseUrl + "?q=" + query; // UNSAFE!

// ❌ Never use Uri.EscapeDataString for full URLs
var url = Uri.EscapeDataString(fullUrl); // WRONG!
```

### JSON Parsing
```csharp
// ❌ Never use IndexOf/LastIndexOf for JSON extraction
var start = text.IndexOf('{');
var json = text.Substring(start); // UNSAFE!

// ❌ Never deserialize without limits
JsonSerializer.Deserialize<T>(json); // UNSAFE without options!
```

### Error Handling
```csharp
// ❌ Never catch Exception without rethrowing or specific handling
catch (Exception ex) { } // WRONG!

// ❌ Never expose exception details to users
return ex.Message; // Information disclosure!

// ❌ Never log sensitive data
_logger.LogError($"API Key: {apiKey}"); // NEVER!
```

### Secrets Management
```csharp
// ❌ Never hardcode API keys
var apiKey = "sk-abc123..."; // NEVER!

// ❌ Never commit secrets to source control
// appsettings.json with API keys // NEVER!

// ❌ Never use config without validation
var key = _options.ApiKey; // Might be null!
```

### HTTP Clients
```csharp
// ❌ Never use HTTP in production
var url = "http://api.example.com"; // UNSAFE!

// ❌ Never share HttpClient headers
_httpClient.DefaultRequestHeaders.Add("Key", key); // Race condition!

// ❌ Never skip timeout configuration
// HttpClient with default timeout // Resource leak risk!
```

### Threading
```csharp
// ❌ Never increment shared counters unsafely
_counter++; // Race condition!

// ❌ Never modify shared collections without locks
list.Add(item); // Race condition in parallel code!
```

---

## Common Patterns

### Adding a New API Tool

1. **Validate inputs**
```csharp
if (!ValidationHelper.IsValidSymbol(symbol))
    throw new ArgumentException(...);
```

2. **Validate configuration**
```csharp
if (string.IsNullOrWhiteSpace(_options.ApiKey))
    throw new InvalidOperationException(...);
if (!ValidationHelper.IsHttpsUrl(_options.BaseUrl))
    throw new InvalidOperationException(...);
```

3. **Configure HttpClient**
```csharp
_httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
```

4. **Use proper error handling**
```csharp
catch (HttpRequestException ex)
{
    throw new ApiDataException(Name, message, ex);
}
catch (TaskCanceledException ex)
{
    throw new ApiTimeoutException(Name, message, ex);
}
```

5. **Enforce resource limits**
```csharp
const int maxResults = 1000;
for (int i = 0; i < Math.Min(count, maxResults); i++)
```

### Adding a New Agent

1. **Sanitize all inputs**
```csharp
var sanitized = ValidationHelper.SanitizeInput(input, maxLength);
```

2. **Use SafeJsonParser for LLM responses**
```csharp
var parsed = SafeJsonParser.ParseJsonFromText<T>(response);
```

3. **Handle concurrency properly**
```csharp
var semaphore = new SemaphoreSlim(3);
// Use semaphore for parallel operations
```

4. **Catch specific exceptions**
```csharp
catch (ToolException ex) { /* handle tool errors */ }
catch (Exception ex) { /* log and handle unexpected */ }
```

---

## Code Review Checklist

When reviewing code, verify:

- [ ] All user inputs validated
- [ ] API keys not hardcoded
- [ ] HTTPS enforced
- [ ] Timeouts configured
- [ ] Exceptions handled specifically
- [ ] No sensitive data in logs
- [ ] Resource limits enforced
- [ ] Thread-safe operations
- [ ] Cancellation tokens used
- [ ] No manual JSON extraction
- [ ] No manual URL concatenation

---

## Security Utilities Reference

### ValidationHelper
- `IsValidSymbol(symbol)` - Validates stock symbols
- `SanitizeCompanyName(name)` - Sanitizes company names
- `SanitizeInput(input, maxLength)` - General input sanitization
- `ValidateDaysBack(days, max)` - Validates date ranges
- `ValidateMaxArticles(count, max)` - Validates article limits
- `IsHttpsUrl(url)` - Validates HTTPS protocol

### SafeJsonParser
- `ParseJsonFromText<T>(content)` - Safely extracts and parses JSON
- `SafeDeserialize<T>(json)` - Deserializes with limits
- `IsValidJson(json)` - Validates JSON structure

### Custom Exceptions
- `ToolException` - Base for all tool errors
- `ApiAuthenticationException` - Auth failures
- `ApiRateLimitException` - Rate limits
- `ApiDataException` - Invalid data
- `ApiTimeoutException` - Timeouts
- `ValidationException` - Input validation failures

---

## Configuration Examples

### Development (secrets.json)
```json
{
  "AlphaVantageOptions:ApiKey": "your-key",
  "NewsApiOptions:ApiKey": "your-key",
  "OpenAI:ApiKey": "your-key"
}
```

### Production (appsettings.json - non-sensitive only)
```json
{
  "AlphaVantageOptions": {
    "BaseUrl": "https://www.alphavantage.co/query",
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  }
}
```

### Environment Variables
```bash
# Windows
$env:AlphaVantageOptions__ApiKey="your-key"

# Linux/Mac
export AlphaVantageOptions__ApiKey="your-key"
```

---

## Need Help?

1. **Setup**: See `SECURITY_SETUP.md`
2. **Summary**: See `SECURITY_FIXES_SUMMARY.md`
3. **Detailed Plan**: See security plan in `.cursor/plans/`
4. **Code Examples**: Check existing implementations in Tools/

---

**Last Updated**: February 5, 2026
