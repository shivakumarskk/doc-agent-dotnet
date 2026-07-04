using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocAgent.Core.Interfaces;

namespace DocAgent.Infrastructure.VectorStores;

/// <summary>
/// ChromaDB Vector Store Adapter
/// </summary>
public class ChromaVectorStore : IVectorStore
{
    private readonly HttpClient _client;
    private readonly string _tenant = "default_tenant";
    private readonly string _database = "default_database";
    private const string ApiVersion = "v2";

    public ChromaVectorStore(string baseUrl = "http://localhost:8000", string tenant = "default_tenant", string database = "default_database")
    {
        baseUrl = baseUrl.TrimEnd('/');
        if (baseUrl.Contains("/api/v2"))
            baseUrl = baseUrl.Replace("/api/v2", "");
        if (baseUrl.Contains("/api"))
            baseUrl = baseUrl.Replace("/api", "");

        _client = new HttpClient { BaseAddress = new Uri($"{baseUrl}/api/{ApiVersion}/") };
        _tenant = tenant;
        _database = database;
    }

    public async Task<List<ChromaCollectionInfo>> ListCollectionsAsync(int? limit = null, int? offset = null)
    {
        var queryParams = new List<string>();
        if (limit.HasValue) queryParams.Add($"limit={limit.Value}");
        if (offset.HasValue) queryParams.Add($"offset={offset.Value}");

        var url = $"tenants/{_tenant}/databases/{_database}/collections";
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<ChromaCollectionInfo>>(resultJson);
        return result ?? new List<ChromaCollectionInfo>();
    }

    public async Task<int> GetCollectionCountAsync()
    {
        var response = await _client.GetAsync($"tenants/{_tenant}/databases/{_database}/collections/count");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return int.Parse(content);
    }

    public async Task<ChromaCollectionInfo> CreateCollectionAsync(string name, Dictionary<string, object>? metadata = null, bool getOrCreate = false)
    {
        var payload = new
        {
            name = name,
            metadata = metadata ?? new Dictionary<string, object>(),
            get_or_create = getOrCreate,
            configuration = (object?)null,
            schema = (object?)null
        };

        var response = await _client.PostAsJsonAsync(
            $"tenants/{_tenant}/databases/{_database}/collections",
            payload
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Chroma API Error: {response.StatusCode} - {errorContent}",
                null,
                response.StatusCode
            );
        }

        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ChromaCollectionInfo>(resultJson) ?? new ChromaCollectionInfo();
    }

    async Task IVectorStore.CreateCollectionAsync(string name, Dictionary<string, object>? metadata)
    {
        await CreateCollectionAsync(name, metadata, false);
    }

    public async Task<ChromaCollectionInfo> GetCollectionAsync(string collectionName)
    {
        var response = await _client.GetAsync($"tenants/{_tenant}/databases/{_database}/collections/{collectionName}");
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ChromaCollectionInfo>(resultJson) ?? new ChromaCollectionInfo();
    }

    public async Task AddAsync(string collectionName, string[] ids, string[] documents, float[][] embeddings, Dictionary<string, object>[]? metadatas = null)
    {
        var collection = await GetCollectionAsync(collectionName);
        if (string.IsNullOrEmpty(collection.Id))
            throw new InvalidOperationException($"Collection '{collectionName}' not found or has no Id.");

        var payload = new
        {
            ids,
            documents,
            embeddings,
            metadatas = metadatas ?? ids.Select((_, i) => new Dictionary<string, object> { { "index", i } }).ToArray()
        };

        var response = await _client.PostAsJsonAsync($"tenants/{_tenant}/databases/{_database}/collections/{collection.Id}/add", payload);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Chroma API Error: {response.StatusCode} - {errorContent}", null, response.StatusCode);
        }
    }

    public async Task<(List<List<string>> Ids, List<List<string>> Documents, List<List<Dictionary<string, object>>> Metadatas)> QueryAsync(
        string collectionName, float[][] queryEmbeddings, int nResults = 3)
    {
        var collection = await GetCollectionAsync(collectionName);
        if (string.IsNullOrEmpty(collection.Id))
            throw new InvalidOperationException($"Collection '{collectionName}' not found or has no Id.");

        var payload = new
        {
            query_embeddings = queryEmbeddings,
            n_results = nResults,
            include = new[] { "documents", "metadatas", "distances", "embeddings" }
        };

        var response = await _client.PostAsJsonAsync($"tenants/{_tenant}/databases/{_database}/collections/{collection.Id}/query", payload);
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ChromaQueryResult>(resultJson) ?? new ChromaQueryResult();

        return (result.Ids, result.Documents, result.Metadatas);
    }

    public async Task<(List<string> Ids, List<string> Documents, List<Dictionary<string, object>> Metadatas)> GetAllAsync(string collectionName)
    {
        var collection = await GetCollectionAsync(collectionName);
        var payload = new
        {
            include = new[] { "documents", "metadatas", "embeddings" }
        };

        var response = await _client.PostAsJsonAsync($"tenants/{_tenant}/databases/{_database}/collections/{collection.Id}/get", payload);
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ChromaGetResult>(resultJson) ?? new ChromaGetResult();

        return (result.Ids, result.Documents, result.Metadatas);
    }

    public async Task DeleteAsync(string collectionName, string[] ids)
    {
        var collection = await GetCollectionAsync(collectionName);
        var payload = new { ids = ids };
        var response = await _client.PostAsJsonAsync($"tenants/{_tenant}/databases/{_database}/collections/{collection.Id}/delete", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> CollectionExistsAsync(string collectionName)
    {
        try
        {
            var collection = await GetCollectionAsync(collectionName);
            return !string.IsNullOrEmpty(collection.Id);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task CreateDatabaseAsync(string databaseName)
    {
        var payload = new { name = databaseName };
        var response = await _client.PostAsJsonAsync(
            $"tenants/{_tenant}/databases",
            payload
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Chroma API Error: {response.StatusCode} - {errorContent}",
                null,
                response.StatusCode
            );
        }
    }

    public async Task UpsertAsync(string collectionName, string[] ids, string[] documents, float[][] embeddings, Dictionary<string, object>[]? metadatas = null)
    {
        var collection = await GetCollectionAsync(collectionName);
        if (string.IsNullOrEmpty(collection.Id))
            throw new InvalidOperationException($"Collection '{collectionName}' not found or has no Id.");

        var payload = new
        {
            ids = ids,
            documents = documents,
            embeddings = embeddings,
            metadatas = metadatas ?? ids.Select((_, i) => new Dictionary<string, object> { { "index", i } }).ToArray()
        };

        var response = await _client.PostAsJsonAsync($"tenants/{_tenant}/databases/{_database}/collections/{collection.Id}/upsert", payload);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Chroma API Error: {response.StatusCode} - {errorContent}", null, response.StatusCode);
        }
    }

    public async Task<bool> DatabaseExistsAsync(string databaseName)
    {
        var response = await _client.GetAsync($"tenants/{_tenant}/databases/{databaseName}");
        return response.IsSuccessStatusCode;
    }
}

public class ChromaQueryResult
{
    [JsonPropertyName("ids")]
    public List<List<string>> Ids { get; set; } = new();

    [JsonPropertyName("documents")]
    public List<List<string>> Documents { get; set; } = new();

    [JsonPropertyName("embeddings")]
    public List<List<List<float>>> Embeddings { get; set; } = new();

    [JsonPropertyName("metadatas")]
    public List<List<Dictionary<string, object>>> Metadatas { get; set; } = new();

    [JsonPropertyName("distances")]
    public List<List<float>> Distances { get; set; } = new();
}

public class ChromaGetResult
{
    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = new();

    [JsonPropertyName("documents")]
    public List<string> Documents { get; set; } = new();

    [JsonPropertyName("embeddings")]
    public List<List<float>> Embeddings { get; set; } = new();

    [JsonPropertyName("metadatas")]
    public List<Dictionary<string, object>> Metadatas { get; set; } = new();
}

public class ChromaCollectionInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("database")]
    public string Database { get; set; } = "";

    [JsonPropertyName("tenant")]
    public string Tenant { get; set; } = "";

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("log_position")]
    public long LogPosition { get; set; }

    [JsonPropertyName("dimension")]
    public int? Dimension { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("configuration_json")]
    public ChromaConfigurationJson? ConfigurationJson { get; set; }

    [JsonPropertyName("schema")]
    public Dictionary<string, object>? Schema { get; set; }
}

public class ChromaConfigurationJson
{
    [JsonPropertyName("embedding_function")]
    public Dictionary<string, object>? EmbeddingFunction { get; set; }

    [JsonPropertyName("hnsw")]
    public Dictionary<string, object>? Hnsw { get; set; }

    [JsonPropertyName("spann")]
    public Dictionary<string, object>? Spann { get; set; }
}
