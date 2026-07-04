using Microsoft.AspNetCore.Http;

namespace DocAgent.Core.Interfaces;

/// <summary>
/// Interface for LLM client (Ollama)
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Generate text using LLM
    /// </summary>
    Task<string> GenerateAsync(string model, string prompt, int maxTokens = 512, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream text generation
    /// </summary>
    IAsyncEnumerable<string> StreamGenerateAsync(string model, string prompt, int maxTokens = 512, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream and collect full response
    /// </summary>
    Task<string> StreamAndCollectAsync(string model, string prompt, int maxTokens, HttpResponse httpResponse, CancellationToken cancellationToken = default);
}
