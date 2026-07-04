namespace DocAgent.Core.DTOs;

/// <summary>
/// DTO for search match results
/// </summary>
public class SearchMatchDto
{
    /// <summary>
    /// Document ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Document text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Document metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// DTO for basic search response
/// </summary>
public class SearchResponseDto
{
    /// <summary>
    /// The search query
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Matched documents from vector store
    /// </summary>
    public List<SearchMatchDto> Matches { get; set; } = new();
}

/// <summary>
/// DTO for search with summary response
/// </summary>
public class SearchWithSummaryResponseDto
{
    /// <summary>
    /// The search query
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// LLM-generated summary answer
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Source documents used for the summary
    /// </summary>
    public List<SearchMatchDto> Matches { get; set; } = new();
}
