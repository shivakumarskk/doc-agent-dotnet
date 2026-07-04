namespace DocAgent.Core.DTOs;

/// <summary>
/// DTO for ingest/upload document requests
/// </summary>
public class IngestDocumentDto
{
    public string Source { get; set; } = string.Empty;
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
}
