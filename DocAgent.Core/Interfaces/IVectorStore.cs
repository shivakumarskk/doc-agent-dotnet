using DocAgent.Core.Models;

namespace DocAgent.Core.Interfaces;

/// <summary>
/// Interface for vector store operations
/// </summary>
public interface IVectorStore
{
    Task AddAsync(string collectionName, string[] ids, string[] documents, float[][] embeddings, Dictionary<string, object>[]? metadatas = null);
    Task<(List<List<string>> Ids, List<List<string>> Documents, List<List<Dictionary<string, object>>> Metadatas)> QueryAsync(string collectionName, float[][] queryEmbeddings, int nResults = 3, Dictionary<string, object>? where = null);
    Task<(List<string> Ids, List<string> Documents, List<Dictionary<string, object>> Metadatas)> GetAllAsync(string collectionName);
    Task DeleteAsync(string collectionName, string[] ids);

    Task<List<VectorCollectionInfo>> ListCollectionsAsync(int? limit = null, int? offset = null);
    Task<int> GetCollectionCountAsync();
    Task<VectorCollectionInfo> GetCollectionAsync(string collectionName);
    Task<bool> CollectionExistsAsync(string collectionName);
    Task CreateCollectionAsync(string name, Dictionary<string, object>? metadata = null);
    Task<VectorCollectionInfo> GetOrCreateCollectionAsync(string collectionName, Dictionary<string, object>? metadata = null);
    Task<VectorCollectionInfo> UpdateCollectionAsync(string collectionName, Dictionary<string, object>? metadata = null, string? newName = null);
    Task DeleteCollectionAsync(string collectionName);
    Task<int> CountAsync(string collectionName);

    Task UpsertAsync(string collectionName, string[] ids, string[] documents, float[][] embeddings, Dictionary<string, object>[]? metadatas = null);
    Task DeleteByFilterAsync(string collectionName, Dictionary<string, object> where);

    Task<bool> DatabaseExistsAsync(string databaseName);
    Task CreateDatabaseAsync(string databaseName);
}
