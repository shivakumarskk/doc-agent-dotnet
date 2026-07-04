namespace DocAgent.Core.Models;

/// <summary>
/// Represents a document in the system
/// </summary>
public class Document
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int ChunkCount { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}
