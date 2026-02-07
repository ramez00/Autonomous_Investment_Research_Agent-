using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AIRA.Agents.LLM;

/// <summary>
/// Groq LLM client - FREE API with fast inference
/// Get API key from: https://console.groq.com
/// Free tier: 14,400 requests/day with llama models
/// </summary>
public class GroqClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqClient> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public GroqClient(
        HttpClient httpClient,
        ILogger<GroqClient> logger,
        string apiKey,
        string model = "llama-3.3-70b-versatile")
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = apiKey;
        _model = model;

        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<string> CompleteChatAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.7,
                max_tokens = 2000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Groq API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<GroqResponse>(responseJson);

            return result?.Choices?[0]?.Message?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Groq API");
            throw;
        }
    }

    private class GroqResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("choices")]
        public List<GroqChoice>? Choices { get; set; }
    }

    private class GroqChoice
    {
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public GroqMessage? Message { get; set; }
    }

    private class GroqMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
