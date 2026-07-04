using DocAgent.Core.Interfaces;
using DocAgent.Core.Models;
using DocAgent.Core.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DocAgent.Application.Services;

/// <summary>
/// Document service for managing document ingestion, querying, and RAG
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILlmClient _llmClient;
    private readonly ITextProcessor _textProcessor;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IConfiguration _configuration;
    private readonly string _defaultCollection = "doc-agent";

    public DocumentService(
        IVectorStore vectorStore,
        IEmbeddingService embeddingService,
        ILlmClient llmClient,
        ITextProcessor textProcessor,
        IFeedbackRepository feedbackRepository,
        IConfiguration configuration)
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _textProcessor = textProcessor ?? throw new ArgumentNullException(nameof(textProcessor));
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<IngestResponseDto> IngestAsync(IngestDocumentDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Extract text from PDF or use provided text
        string text = File.Exists(request.Source)
            ? _textProcessor.ExtractTextFromPdf(request.Source)
            : request.Source;

        // Chunk text
        var chunks = _textProcessor.ChunkText(text, request.ChunkSize, request.ChunkOverlap).ToList();

        // Generate embeddings and prepare batch data
        var docId = Path.GetFileNameWithoutExtension(request.Source);
        var ids = new List<string>();
        var documents = new List<string>();
        var embeddings = new List<float[]>();
        var metadatas = new List<Dictionary<string, object>>();

        foreach (var chunk in chunks)
        {
            var uniqueId = $"{docId}-{chunk.Id}";
            var vector = _embeddingService.Embed(chunk.Text);

            ids.Add(uniqueId);
            documents.Add(chunk.Text);
            embeddings.Add(vector);
            metadatas.Add(new Dictionary<string, object> { { "document_id", docId } });
        }

        // Add to ChromaDB
        await _vectorStore.AddAsync(_defaultCollection, ids.ToArray(), documents.ToArray(), embeddings.ToArray(), metadatas.ToArray());

        return new IngestResponseDto
        {
            DocumentId = docId,
            TotalChunks = chunks.Count,
            UploadedAt = DateTime.UtcNow
        };
    }

    public async Task<List<DocumentDto>> GetAllDocumentsAsync()
    {
        var (ids, _, metadatas) = await _vectorStore.GetAllAsync(_defaultCollection);

        var uniqueDocs = metadatas
            .Where(m => m.ContainsKey("document_id"))
            .Select(m => m["document_id"].ToString() ?? "")
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        return uniqueDocs.Select(docId => new DocumentDto
        {
            Id = docId,
            Name = docId,
            ChunkCount = metadatas.Count(m => m.ContainsKey("document_id") && m["document_id"].ToString() == docId),
            UploadedAt = DateTime.UtcNow
        }).ToList();
    }

    public async Task<QueryResponseDto> QueryAsync(QueryRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var qVec = _embeddingService.Embed(request.Question);
        var (ids, documents, metadatas) = await _vectorStore.QueryAsync(_defaultCollection, new[] { qVec }, request.TopK);

        var contextChunks = new List<ContextChunkDto>();
        if (ids.Count > 0)
        {
            for (int i = 0; i < ids[0].Count; i++)
            {
                contextChunks.Add(new ContextChunkDto
                {
                    Id = ids[0][i],
                    DocumentId = ids[0][i].Split('-')[0],
                    Preview = documents[0][i].Substring(0, Math.Min(200, documents[0][i].Length))
                });
            }
        }

        return new QueryResponseDto
        {
            Question = request.Question,
            ContextChunks = contextChunks
        };
    }

    public async Task<AskResponseDto> AskAsync(AskRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Embed the question
        var qVec = _embeddingService.Embed(request.Question);

        // 2. Retrieve top-K chunks from vector store
        var (ids, documents, _) = await _vectorStore.QueryAsync(_defaultCollection, new[] { qVec }, request.TopK);

        // 3. Build context from results
        var contextParts = new List<string>();
        var contextChunks = new List<ContextChunkDto>();

        if (ids.Count > 0 && documents.Count > 0)
        {
            for (int i = 0; i < ids[0].Count; i++)
            {
                var id = ids[0][i];
                var doc = documents[0][i];
                var docId = id.Split('-')[0];
                contextParts.Add($"[Doc: {docId}] {doc}");
                contextChunks.Add(new ContextChunkDto
                {
                    Id = id,
                    DocumentId = docId,
                    Preview = doc.Substring(0, Math.Min(200, doc.Length))
                });
            }
        }

        var context = string.Join("\n---\n", contextParts);

        // 4. Build prompt for LLM
        var prompt = $@"
You are a helpful assistant. Answer the question using ONLY the provided context. 
If the context does not contain enough information, say: 
'I cannot answer based on the available documents.'

Context:
{context}

Question:
{request.Question}

Answer in 3–5 sentences. 
Always mention the document ID when referencing information.
";

        // 5. Call LLM
        var answer = await _llmClient.GenerateAsync("llama3", prompt, request.MaxTokens);

        return new AskResponseDto
        {
            Question = request.Question,
            ContextChunks = contextChunks,
            Answer = answer
        };
    }

    public async Task<AskResponseDto> AskStreamAsync(AskRequestDto request, HttpResponse response, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var qVec = _embeddingService.Embed(request.Question);
        var (ids, documents, _) = await _vectorStore.QueryAsync(_defaultCollection, new[] { qVec }, request.TopK);

        var context = documents.Count > 0 ? string.Join("\n\n", documents[0]) : "";
        var contextChunks = new List<ContextChunkDto>();

        if (ids.Count > 0)
        {
            for (int i = 0; i < ids[0].Count; i++)
            {
                contextChunks.Add(new ContextChunkDto
                {
                    Id = ids[0][i],
                    DocumentId = ids[0][i].Split('-')[0]
                });
            }
        }

        var prompt = $@"
You are a helpful assistant. Answer the question using ONLY the provided context. 
If the context does not contain enough information, say: 
'I cannot answer based on the available documents.'

Context:
{context}

Question:
{request.Question}

Answer in 3–5 sentences. 
Always mention the document ID when referencing information.
";

        var fullAnswer = await _llmClient.StreamAndCollectAsync("llama3", prompt, request.MaxTokens, response, cancellationToken);

        return new AskResponseDto
        {
            Question = request.Question,
            ContextChunks = contextChunks,
            Answer = fullAnswer
        };
    }

    public async Task<bool> DeleteDocumentAsync(string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            throw new ArgumentException("Document ID cannot be empty", nameof(documentId));

        var (allIds, _, metadatas) = await _vectorStore.GetAllAsync(_defaultCollection);

        var idsToDelete = allIds
            .Where((id, idx) =>
                idx < metadatas.Count &&
                metadatas[idx].ContainsKey("document_id") &&
                metadatas[idx]["document_id"].ToString() == documentId)
            .ToArray();

        if (idsToDelete.Length > 0)
        {
            await _vectorStore.DeleteAsync(_defaultCollection, idsToDelete);
        }

        return idsToDelete.Length > 0;
    }

    public async Task<IngestResponseDto> UpdateDocumentAsync(IngestDocumentDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var docId = Path.GetFileNameWithoutExtension(request.Source);

        // Step 1: Delete old records
        await DeleteDocumentAsync(docId);

        // Step 2: Ingest updated document
        return await IngestAsync(request);
    }

    public async Task<SearchResponseDto> SearchAsync(SearchRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Query))
            throw new ArgumentException("Query cannot be empty", nameof(request.Query));

        // 1. Embed the query
        var queryEmbedding = _embeddingService.Embed(request.Query);

        // 2. Query vector store
        var (ids, documents, metadatas) = await _vectorStore.QueryAsync(
            _defaultCollection,
            new[] { queryEmbedding },
            request.TopK
        );

        // 3. Build matches from results
        var matches = new List<SearchMatchDto>();
        if (ids.Count > 0 && documents.Count > 0)
        {
            for (int i = 0; i < ids[0].Count; i++)
            {
                matches.Add(new SearchMatchDto
                {
                    Id = ids[0][i],
                    Text = documents[0][i],
                    Metadata = metadatas.Count > 0 && metadatas[0].Count > i ? metadatas[0][i] : null
                });
            }
        }

        return new SearchResponseDto
        {
            Query = request.Query,
            Matches = matches
        };
    }

    public async Task<SearchWithSummaryResponseDto> SearchWithSummaryAsync(SearchRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Query))
            throw new ArgumentException("Query cannot be empty", nameof(request.Query));

        // 1. Embed the query
        var queryEmbedding = _embeddingService.Embed(request.Query);

        // 2. Query vector store for relevant documents
        var (ids, documents, metadatas) = await _vectorStore.QueryAsync(
            _defaultCollection,
            new[] { queryEmbedding },
            request.TopK
        );

        // 3. Extract document texts and build context
        var relevantDocuments = new List<string>();
        if (documents.Count > 0 && documents[0].Count > 0)
        {
            relevantDocuments.AddRange(documents[0]);
        }

        // 4. Build prompt and call LLM for summarization
        var context = string.Join("\n\n", relevantDocuments);
        var prompt = $"Answer the question based on the following context:\n\n{context}\n\nQuestion: {request.Query}\nAnswer:";

        var modelName = _configuration["Ollama:Model"] ?? "llama2";
        var summary = await _llmClient.GenerateAsync(modelName, prompt);

        // 5. Build matches from results
        var matches = new List<SearchMatchDto>();
        if (ids.Count > 0 && documents.Count > 0)
        {
            for (int i = 0; i < ids[0].Count; i++)
            {
                matches.Add(new SearchMatchDto
                {
                    Id = ids[0][i],
                    Text = documents[0][i],
                    Metadata = metadatas.Count > 0 && metadatas[0].Count > i ? metadatas[0][i] : null
                });
            }
        }

        return new SearchWithSummaryResponseDto
        {
            Query = request.Query,
            Summary = summary,
            Matches = matches
        };
    }
}
