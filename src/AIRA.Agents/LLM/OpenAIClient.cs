using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace AIRA.Agents.LLM;

/// <summary>
/// OpenAI LLM client wrapper
/// </summary>
public class OpenAILlmClient : ILlmClient
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAILlmClient> _logger;

    public OpenAILlmClient(ChatClient chatClient, ILogger<OpenAILlmClient> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<string> CompleteChatAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            throw;
        }
    }
}
