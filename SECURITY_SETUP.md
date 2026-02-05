# Security Setup Guide

This guide covers the security enhancements implemented in the AIRA.Agents project and how to configure them properly.

## Overview

The following security measures have been implemented:

1. ✅ **API Key Management** - Secure validation and handling
2. ✅ **Input Validation** - Prevention of injection attacks
3. ✅ **HTTPS Enforcement** - All external communications use secure protocols
4. ✅ **Error Handling** - Safe error messages without information disclosure
5. ✅ **Thread Safety** - Concurrent operation safety
6. ✅ **JSON Parsing** - Safe deserialization with depth and size limits
7. ✅ **Prompt Injection Prevention** - Sanitization of LLM inputs
8. ✅ **Rate Limiting** - Resource protection mechanisms

---

## 1. Secrets Management

### Development Environment (User Secrets)

**Recommended for local development:**

```bash
# Initialize user secrets
cd src/AIRA.Agents
dotnet user-secrets init

# Add API keys
dotnet user-secrets set "AlphaVantageOptions:ApiKey" "your-alphavantage-key"
dotnet user-secrets set "NewsApiOptions:ApiKey" "your-newsapi-key"
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-key"
```

User secrets are stored in:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **Linux/Mac**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

### Production Environment (Azure Key Vault)

**Recommended for production:**

1. **Create Azure Key Vault:**
```bash
az keyvault create \
  --name aira-keyvault \
  --resource-group aira-rg \
  --location eastus
```

2. **Add Secrets:**
```bash
az keyvault secret set \
  --vault-name aira-keyvault \
  --name AlphaVantageOptions--ApiKey \
  --value "your-api-key"

az keyvault secret set \
  --vault-name aira-keyvault \
  --name NewsApiOptions--ApiKey \
  --value "your-api-key"
```

Note: Use `--` (double dash) in Azure Key Vault secret names to represent `:` in configuration keys.

3. **Install Required Packages:**
```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

4. **Configure in Program.cs:**
```csharp
using Azure.Identity;

var keyVaultName = builder.Configuration["KeyVaultName"];
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential()
);
```

5. **Grant Access:**
- Use **Managed Identity** (recommended) for Azure-hosted apps
- Or create a **Service Principal** with Key Vault access

---

## 2. Input Validation

All user inputs are validated to prevent injection attacks:

### Stock Symbol Validation
- Format: `^[A-Z0-9.-]{1,10}$`
- Maximum length: 10 characters
- Allowed characters: uppercase letters, numbers, dots, hyphens

### Company Name Sanitization
- Control characters removed
- Maximum length: 200 characters
- Prompt injection patterns filtered

### Example Usage:
```csharp
// Automatic validation in all tools
var data = await financialTool.GetQuoteAsync("AAPL"); // Valid
var data = await financialTool.GetQuoteAsync("'; DROP TABLE--"); // Throws ArgumentException
```

---

## 3. HTTPS Enforcement

All API base URLs are validated to use HTTPS:

```csharp
// In appsettings.json
{
  "AlphaVantageOptions": {
    "BaseUrl": "https://www.alphavantage.co/query",  // ✅ Valid
    "ApiKey": "configured-via-secrets"
  },
  "NewsApiOptions": {
    "BaseUrl": "http://newsapi.org/v2"  // ❌ Will throw exception
  }
}
```

HTTP clients are configured with:
- **Timeout**: 30 seconds (configurable)
- **Connection limits**: Per HttpClient best practices
- **Retry policies**: Recommended via Polly (see below)

---

## 4. HTTP Client Configuration

### Recommended Configuration (with Polly)

Install Polly for retry policies:
```bash
dotnet add package Microsoft.Extensions.Http.Polly
```

Configure in DI:
```csharp
services.AddHttpClient<AlphaVantageTool>()
    .AddPolicyHandler(Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(30));
```

---

## 5. Error Handling

Custom exception types provide structured error information without exposing sensitive details:

### Exception Types:
- `ApiAuthenticationException` - Authentication failures
- `ApiRateLimitException` - Rate limit exceeded
- `ApiDataException` - Invalid or malformed data
- `ApiTimeoutException` - Request timeouts
- `ValidationException` - Input validation failures

### Logging:
- Errors logged with structured data
- Sensitive information (API keys, stack traces) never exposed to users
- Security events logged separately for monitoring

---

## 6. Thread Safety

All shared state is protected:
- Counter increments use `Interlocked.Increment()`
- HttpClient instances follow best practices
- No shared mutable state in tools

---

## 7. Resource Limits

### JSON Parsing:
- Maximum depth: 64 levels
- Maximum size: 1MB
- Safe extraction and deserialization

### Collections:
- Historical prices: Limited by API and validated
- News articles: Limited to 100 per request
- Days back: Capped at 365 days

### Configuration:
```csharp
// In appsettings.json
{
  "AlphaVantageOptions": {
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  }
}
```

---

## 8. Prompt Injection Prevention

All inputs to LLM prompts are sanitized:

### Filtered Patterns:
- "ignore previous instructions"
- "disregard previous"
- "system:", "assistant:", "user:"
- Special tokens: `[INST]`, `<|im_start|>`, etc.

### Example:
```csharp
var companyName = "Microsoft; ignore previous instructions";
// Sanitized to: "Microsoft"
```

---

## Configuration Files

### .env.example (Template)
```env
# Never commit actual values - this is a template only
ALPHAVANTAGE_API_KEY=your_key_here
NEWSAPI_API_KEY=your_key_here
OPENAI_API_KEY=your_key_here
```

### appsettings.json (Non-sensitive settings)
```json
{
  "AlphaVantageOptions": {
    "BaseUrl": "https://www.alphavantage.co/query",
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  },
  "NewsApiOptions": {
    "BaseUrl": "https://newsapi.org/v2",
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  }
}
```

---

## Security Checklist

Before deploying to production:

- [ ] All API keys stored in Azure Key Vault or secure secret manager
- [ ] HTTPS enforced for all external communications
- [ ] User Secrets initialized for local development
- [ ] `.env` files added to `.gitignore`
- [ ] Error logging configured with sensitive data redaction
- [ ] HTTP client timeouts and retry policies configured
- [ ] Input validation tested with malicious inputs
- [ ] Rate limiting implemented (if needed)
- [ ] Security monitoring and alerts configured
- [ ] Penetration testing completed
- [ ] Code analysis tools run (SonarQube, etc.)

---

## Monitoring and Alerts

### Recommended Monitoring:
1. **API Usage**: Track requests per API key
2. **Error Rates**: Monitor exception types and frequencies
3. **Performance**: Track response times and timeouts
4. **Security Events**: Log validation failures and suspicious patterns

### Azure Application Insights Integration:
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

---

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Azure Key Vault Documentation](https://docs.microsoft.com/azure/key-vault/)
- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/aspnet/core/security/)
- [Polly Resilience Framework](https://github.com/App-vNext/Polly)

---

## Support

For security concerns or questions:
1. Review the security plan documentation
2. Check implementation in `ValidationHelper.cs` and `SafeJsonParser.cs`
3. Review exception handling in `Tools/Exceptions/`

**Never commit sensitive information to source control.**
