namespace DocAgent.Core.DTOs;

/// <summary>
/// DTO for creating or getting a collection.
/// </summary>
public class CollectionCreateRequestDto
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// DTO for updating a collection.
/// </summary>
public class CollectionUpdateRequestDto
{
    public string? NewName { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// DTO for upserting records in a collection.
/// </summary>
public class CollectionUpsertRequestDto
{
    public string[] Ids { get; set; } = [];
    public string[] Documents { get; set; } = [];
    public float[][] Embeddings { get; set; } = [];
    public Dictionary<string, object>[]? Metadatas { get; set; }
}

/// <summary>
/// DTO for deleting records by filter.
/// </summary>
public class CollectionDeleteByFilterRequestDto
{
    public Dictionary<string, object> Where { get; set; } = new();
}

/// <summary>
/// DTO for creating a Chroma database.
/// </summary>
public class DatabaseCreateRequestDto
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// DTO for checking database existence.
/// </summary>
public class DatabaseExistsResponseDto
{
    public string Name { get; set; } = string.Empty;
    public bool Exists { get; set; }
}

/// <summary>
/// DTO for collection diagnostics.
/// </summary>
public class CollectionDiagnosticsDto
{
    public string DatabaseName { get; set; } = string.Empty;
    public bool DatabaseExists { get; set; }
    public int CollectionCount { get; set; }
    public List<string> MissingDefaultCollections { get; set; } = [];
    public bool Ready { get; set; }
}