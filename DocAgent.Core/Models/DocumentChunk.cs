namespace DocAgent.Core.Models;

/// <summary>
/// Represents a document chunk with embeddings
/// </summary>
public class DocumentChunk
{
    public string Id { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public float[]? Embedding { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
