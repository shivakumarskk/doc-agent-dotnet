namespace DocAgent.Core.DTOs;

/// <summary>
/// DTO for search requests
/// </summary>
public class SearchRequestDto
{
    /// <summary>
    /// The search query
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Number of results to return (default: 5)
    /// </summary>
    public int TopK { get; set; } = 5;
}
