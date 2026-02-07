using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AIRA.Agents.LLM;

/// <summary>
/// Ollama LLM client - FREE local LLM provider
/// Download from: https://ollama.ai
/// </summary>
public class OllamaClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaClient> _logger;
    private readonly string _model;
    private readonly string _baseUrl;

    public OllamaClient(
        HttpClient httpClient,
        ILogger<OllamaClient> logger,
        string model = "llama3.2",
        string baseUrl = "http://localhost:11434")
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = model;
        _baseUrl = baseUrl;
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
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/chat",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

            return result?.Message?.Content ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Ollama. Is Ollama running?");
            throw new InvalidOperationException(
                "Cannot connect to Ollama. Please install and start Ollama from https://ollama.ai", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama API");
            throw;
        }
    }

    private class OllamaResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }
    }

    private class OllamaMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
