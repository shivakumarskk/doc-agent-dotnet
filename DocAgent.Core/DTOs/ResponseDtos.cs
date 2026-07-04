namespace DocAgent.Core.DTOs;

/// <summary>
/// DTO for document list responses
/// </summary>
public class DocumentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// DTO for query response
/// </summary>
public class QueryResponseDto
{
    public string Question { get; set; } = string.Empty;
    public List<ContextChunkDto> ContextChunks { get; set; } = new();
}

/// <summary>
/// DTO for context chunks in responses
/// </summary>
public class ContextChunkDto
{
    public string Id { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public float? Distance { get; set; }
}

/// <summary>
/// DTO for ask/RAG response
/// </summary>
public class AskResponseDto
{
    public string Question { get; set; } = string.Empty;
    public List<ContextChunkDto> ContextChunks { get; set; } = new();
    public string Answer { get; set; } = string.Empty;
}

/// <summary>
/// DTO for ingest/upload response
/// </summary>
public class IngestResponseDto
{
    public string DocumentId { get; set; } = string.Empty;
    public int TotalChunks { get; set; }
    public DateTime UploadedAt { get; set; }
}
