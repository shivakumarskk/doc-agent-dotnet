namespace DocAgent.Core.Models;

/// <summary>
/// Represents a feedback record for query results
/// </summary>
public class Feedback
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string QueryId { get; set; } = string.Empty;
    public List<string> ContextChunkIds { get; set; } = new();
    public bool IsPositive { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
