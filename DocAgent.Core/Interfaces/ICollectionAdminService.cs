using DocAgent.Core.DTOs;
using DocAgent.Core.Models;

namespace DocAgent.Core.Interfaces;

/// <summary>
/// Interface for Chroma collections and database administration.
/// </summary>
public interface ICollectionAdminService
{
    Task<List<VectorCollectionInfo>> ListCollectionsAsync(int? limit = null, int? offset = null);
    Task<int> GetCollectionCountAsync();
    Task<VectorCollectionInfo> GetCollectionAsync(string collectionName);
    Task<VectorCollectionInfo> CreateCollectionAsync(CollectionCreateRequestDto request);
    Task<VectorCollectionInfo> GetOrCreateCollectionAsync(CollectionCreateRequestDto request);
    Task<VectorCollectionInfo> UpdateCollectionAsync(string collectionName, CollectionUpdateRequestDto request);
    Task DeleteCollectionAsync(string collectionName);

    Task<int> GetRecordCountAsync(string collectionName);
    Task UpsertRecordsAsync(string collectionName, CollectionUpsertRequestDto request);
    Task DeleteRecordsByFilterAsync(string collectionName, CollectionDeleteByFilterRequestDto request);

    Task<DatabaseExistsResponseDto> DatabaseExistsAsync(string databaseName);
    Task CreateDatabaseAsync(DatabaseCreateRequestDto request);
    Task<CollectionDiagnosticsDto> GetDiagnosticsAsync();
}