namespace DocAgent.Core.Models;

/// <summary>
/// Core model representing a vector collection.
/// </summary>
public class VectorCollectionInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Tenant { get; set; } = string.Empty;
    public int Version { get; set; }
    public long LogPosition { get; set; }
    public int? Dimension { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}