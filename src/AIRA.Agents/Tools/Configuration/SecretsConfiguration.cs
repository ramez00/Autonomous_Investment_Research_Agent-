using Microsoft.Extensions.Configuration;

namespace AIRA.Agents.Tools.Configuration;

/// <summary>
/// Helper class for validating secrets configuration
/// </summary>
/// <remarks>
/// For full secrets management setup (User Secrets, Azure Key Vault, etc.),
/// see SECURITY_SETUP.md in the project root.
/// 
/// Required NuGet packages for full implementation:
/// - Microsoft.Extensions.Configuration.UserSecrets (for development)
/// - Microsoft.Extensions.Configuration.EnvironmentVariables (for all environments)
/// - Azure.Extensions.AspNetCore.Configuration.Secrets (for production)
/// - Azure.Identity (for Azure authentication)
/// </remarks>
public static class SecretsConfiguration
{
    /// <summary>
    /// Validates that required secrets are configured
    /// </summary>
    /// <param name="configuration">The configuration to validate</param>
    /// <param name="requiredKeys">Keys that must be present and non-empty</param>
    /// <exception cref="InvalidOperationException">Thrown when required keys are missing</exception>
    public static void ValidateRequiredSecrets(IConfiguration configuration, params string[] requiredKeys)
    {
        var missingKeys = new List<string>();
        
        foreach (var key in requiredKeys)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                missingKeys.Add(key);
            }
        }
        
        if (missingKeys.Any())
        {
            throw new InvalidOperationException(
                $"Required configuration keys are missing: {string.Join(", ", missingKeys)}. " +
                "Please configure them in User Secrets (development) or Azure Key Vault (production). " +
                "See SECURITY_SETUP.md for detailed instructions.");
        }
    }
}

/// <summary>
/// Instructions for setting up secure secrets management
/// </summary>
public static class SecretsSetupInstructions
{
    public const string UserSecretsInstructions = @"
# Setting up User Secrets (Development)

1. Right-click on the project in Visual Studio and select 'Manage User Secrets'
   Or run: dotnet user-secrets init

2. Add your API keys to secrets.json:
{
  ""AlphaVantageOptions:ApiKey"": ""your-api-key-here"",
  ""NewsApiOptions:ApiKey"": ""your-api-key-here"",
  ""OpenAI:ApiKey"": ""your-api-key-here""
}

3. User secrets are stored outside the project directory and never committed to source control.
";

    public const string AzureKeyVaultInstructions = @"
# Setting up Azure Key Vault (Production)

1. Create an Azure Key Vault:
   az keyvault create --name your-keyvault-name --resource-group your-rg --location eastus

2. Add secrets to Key Vault:
   az keyvault secret set --vault-name your-keyvault-name --name AlphaVantageOptions--ApiKey --value ""your-api-key""
   az keyvault secret set --vault-name your-keyvault-name --name NewsApiOptions--ApiKey --value ""your-api-key""
   az keyvault secret set --vault-name your-keyvault-name --name OpenAI--ApiKey --value ""your-api-key""

   Note: Use '--' (double dash) in secret names to represent ':' (colon) in configuration keys.

3. Grant access to your application:
   - Use Managed Identity (recommended for Azure-hosted apps)
   - Or use Service Principal with appropriate permissions

4. Add NuGet packages:
   dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
   dotnet add package Azure.Identity

5. Configure in Program.cs:
   builder.Configuration.AddAzureKeyVault(
       new Uri($""https://{keyVaultName}.vault.azure.net/""),
       new DefaultAzureCredential()
   );
";

    public const string EnvironmentVariablesInstructions = @"
# Setting up Environment Variables

Environment variables have the highest priority and override all other sources.

## Windows (PowerShell):
$env:AlphaVantageOptions__ApiKey=""your-api-key""
$env:NewsApiOptions__ApiKey=""your-api-key""

## Linux/Mac (bash):
export AlphaVantageOptions__ApiKey=""your-api-key""
export NewsApiOptions__ApiKey=""your-api-key""

Note: Use '__' (double underscore) to represent ':' (colon) in configuration keys.

## Docker:
docker run -e AlphaVantageOptions__ApiKey=your-key your-image

## Kubernetes:
Create a Secret and mount it as environment variables in your deployment.
";

    public const string SecurityBestPractices = @"
# Security Best Practices for API Keys

1. NEVER commit API keys to source control
   - Use .gitignore to exclude config files with secrets
   - Use .env.example as template (without actual keys)

2. Use different keys for different environments
   - Development, Staging, Production should have separate keys

3. Rotate keys regularly
   - Set up key rotation schedules
   - Update all environments when rotating

4. Monitor API key usage
   - Set up alerts for unusual activity
   - Review API usage logs regularly

5. Use least privilege principle
   - Grant only necessary permissions to API keys
   - Use separate keys for different services

6. Encrypt secrets at rest
   - Azure Key Vault automatically encrypts secrets
   - Ensure database encryption is enabled

7. Use managed identities when possible
   - Eliminates need to manage credentials
   - Recommended for Azure-hosted applications
";
}
