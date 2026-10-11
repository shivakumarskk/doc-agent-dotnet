using DocAgent.Core.DTOs;
using DocAgent.Core.Interfaces;
using DocAgent.Core.Models;
using Microsoft.Extensions.Configuration;

namespace DocAgent.Application.Services;

/// <summary>
/// Service for Chroma collection and database administration.
/// </summary>
public class CollectionAdminService : ICollectionAdminService
{
    private static readonly HashSet<string> ProtectedCollections = new(StringComparer.OrdinalIgnoreCase)
    {
        "documents",
        "users"
    };

    private readonly IVectorStore _vectorStore;
    private readonly string _defaultDatabase;

    public CollectionAdminService(IVectorStore vectorStore, IConfiguration configuration)
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        ArgumentNullException.ThrowIfNull(configuration);
        _defaultDatabase = configuration["ChromaDB:Database"] ?? "default_database";
    }

    public Task<List<VectorCollectionInfo>> ListCollectionsAsync(int? limit = null, int? offset = null)
    {
        return _vectorStore.ListCollectionsAsync(limit, offset);
    }

    public Task<int> GetCollectionCountAsync()
    {
        return _vectorStore.GetCollectionCountAsync();
    }

    public Task<VectorCollectionInfo> GetCollectionAsync(string collectionName)
    {
        ValidateCollectionName(collectionName);
        return _vectorStore.GetCollectionAsync(collectionName);
    }

    public Task<VectorCollectionInfo> CreateCollectionAsync(CollectionCreateRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCollectionName(request.Name);
        return _vectorStore.GetOrCreateCollectionAsync(request.Name, request.Metadata);
    }

    public Task<VectorCollectionInfo> GetOrCreateCollectionAsync(CollectionCreateRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCollectionName(request.Name);
        return _vectorStore.GetOrCreateCollectionAsync(request.Name, request.Metadata);
    }

    public Task<VectorCollectionInfo> UpdateCollectionAsync(string collectionName, CollectionUpdateRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCollectionName(collectionName);

        if (ProtectedCollections.Contains(collectionName) &&
            !string.IsNullOrWhiteSpace(request.NewName) &&
            !string.Equals(collectionName, request.NewName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Collection '{collectionName}' is protected and cannot be renamed.");
        }

        if (!string.IsNullOrWhiteSpace(request.NewName))
        {
            ValidateCollectionName(request.NewName);
        }

        return _vectorStore.UpdateCollectionAsync(collectionName, request.Metadata, request.NewName?.Trim());
    }

    public async Task DeleteCollectionAsync(string collectionName)
    {
        ValidateCollectionName(collectionName);

        if (ProtectedCollections.Contains(collectionName))
        {
            throw new InvalidOperationException($"Collection '{collectionName}' is protected and cannot be deleted.");
        }

        await _vectorStore.DeleteCollectionAsync(collectionName);
    }

    public Task<int> GetRecordCountAsync(string collectionName)
    {
        ValidateCollectionName(collectionName);
        return _vectorStore.CountAsync(collectionName);
    }

    public Task UpsertRecordsAsync(string collectionName, CollectionUpsertRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCollectionName(collectionName);

        if (request.Ids.Length == 0)
            throw new ArgumentException("At least one record is required.", nameof(request));

        if (request.Documents.Length != request.Ids.Length || request.Embeddings.Length != request.Ids.Length)
            throw new ArgumentException("Ids, documents, and embeddings counts must match.", nameof(request));

        if (request.Metadatas is not null && request.Metadatas.Length != request.Ids.Length)
            throw new ArgumentException("Metadatas count must match record count.", nameof(request));

        return _vectorStore.UpsertAsync(collectionName, request.Ids, request.Documents, request.Embeddings, request.Metadatas);
    }

    public Task DeleteRecordsByFilterAsync(string collectionName, CollectionDeleteByFilterRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCollectionName(collectionName);

        if (request.Where.Count == 0)
            throw new ArgumentException("Where filter is required.", nameof(request));

        return _vectorStore.DeleteByFilterAsync(collectionName, request.Where);
    }

    public async Task<DatabaseExistsResponseDto> DatabaseExistsAsync(string databaseName)
    {
        ValidateDatabaseName(databaseName);
        var exists = await _vectorStore.DatabaseExistsAsync(databaseName.Trim());
        return new DatabaseExistsResponseDto
        {
            Name = databaseName.Trim(),
            Exists = exists
        };
    }

    public async Task CreateDatabaseAsync(DatabaseCreateRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateDatabaseName(request.Name);

        await _vectorStore.CreateDatabaseAsync(request.Name.Trim());
    }

    public async Task<CollectionDiagnosticsDto> GetDiagnosticsAsync()
    {
        var databaseExists = await _vectorStore.DatabaseExistsAsync(_defaultDatabase);

        var collectionCount = 0;
        List<VectorCollectionInfo> collections = [];
        if (databaseExists)
        {
            collectionCount = await _vectorStore.GetCollectionCountAsync();
            collections = await _vectorStore.ListCollectionsAsync();
        }

        var existingNames = collections.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = ProtectedCollections.Where(c => !existingNames.Contains(c)).ToList();

        return new CollectionDiagnosticsDto
        {
            DatabaseName = _defaultDatabase,
            DatabaseExists = databaseExists,
            CollectionCount = collectionCount,
            MissingDefaultCollections = missing,
            Ready = databaseExists && missing.Count == 0
        };
    }

    private static void ValidateCollectionName(string? collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
            throw new ArgumentException("Collection name cannot be empty.", nameof(collectionName));
    }

    private static void ValidateDatabaseName(string? databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name cannot be empty.", nameof(databaseName));
    }
}