namespace DocAgent.Core.DTOs;

/// <summary>
/// DTO for feedback submission
/// </summary>
public class FeedbackDto
{
    public string QueryId { get; set; } = string.Empty;
    public List<string> ContextChunkIds { get; set; } = new();
    public bool IsPositive { get; set; }
    public string? Comment { get; set; }
}
