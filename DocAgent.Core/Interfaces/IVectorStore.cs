namespace DocAgent.Core.Interfaces;

/// <summary>
/// Interface for vector store operations
/// </summary>
public interface IVectorStore
{
    Task AddAsync(string collectionName, string[] ids, string[] documents, float[][] embeddings, Dictionary<string, object>[]? metadatas = null);
    Task<(List<List<string>> Ids, List<List<string>> Documents, List<List<Dictionary<string, object>>> Metadatas)> QueryAsync(string collectionName, float[][] queryEmbeddings, int nResults = 3);
    Task<(List<string> Ids, List<string> Documents, List<Dictionary<string, object>> Metadatas)> GetAllAsync(string collectionName);
    Task DeleteAsync(string collectionName, string[] ids);
    Task<bool> CollectionExistsAsync(string collectionName);
    Task CreateCollectionAsync(string name, Dictionary<string, object>? metadata = null);

    Task<bool> DatabaseExistsAsync(string databaseName);

    Task CreateDatabaseAsync(string databaseName);
}
