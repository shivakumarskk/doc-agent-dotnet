using DocAgent.Core.Interfaces;

namespace DocAgent.Infrastructure.Services;

/// <summary>
/// Null implementation of IEmbeddingService used when model files are not available
/// </summary>
public class NullEmbeddingService : IEmbeddingService
{
    /// <summary>
    /// Returns a zero-filled embedding vector
    /// </summary>
    public float[] Embed(string text)
    {
        // Return a default 384-dimensional zero vector (common embedding size)
        // This allows the application to run without the model, though embeddings won't be meaningful
        return new float[384];
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
