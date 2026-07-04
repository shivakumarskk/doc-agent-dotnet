namespace DocAgent.Core.Models;

/// <summary>
/// Represents a query result with context and answer
/// </summary>
public class QueryResult
{
    public string Question { get; set; } = string.Empty;
    public List<ContextChunk> ContextChunks { get; set; } = new();
    public string Answer { get; set; } = string.Empty;
    public float? Confidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a context chunk returned in a query
/// </summary>
public class ContextChunk
{
    public string Id { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public float? Distance { get; set; }
}
