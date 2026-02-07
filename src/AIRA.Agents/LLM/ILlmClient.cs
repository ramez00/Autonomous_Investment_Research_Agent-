namespace AIRA.Agents.LLM;

/// <summary>
/// Abstraction for LLM providers (OpenAI, Ollama, etc.)
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Complete a chat conversation with the given messages
    /// </summary>
    Task<string> CompleteChatAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
