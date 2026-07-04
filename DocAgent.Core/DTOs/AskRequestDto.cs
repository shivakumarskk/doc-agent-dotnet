namespace DocAgent.Core.DTOs;

/// <summary>
/// DTO for RAG (Retrieval Augmented Generation) ask requests
/// </summary>
public class AskRequestDto
{
    public string Question { get; set; } = string.Empty;
    public int TopK { get; set; } = 3;
    public int MaxTokens { get; set; } = 1024;
    public string? DocumentFilter { get; set; }
}
