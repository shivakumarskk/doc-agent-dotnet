namespace DocAgent.Core.DTOs;

/// <summary>
/// DTO for query requests
/// </summary>
public class QueryRequestDto
{
    public string Question { get; set; } = string.Empty;
    public int TopK { get; set; } = 3;
    public string? DocumentFilter { get; set; }
}
