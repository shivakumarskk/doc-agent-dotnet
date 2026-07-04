namespace DocAgent.Core.Interfaces;

/// <summary>
/// Interface for embedding service
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate embedding for text
    /// </summary>
    float[] Embed(string text);
}
