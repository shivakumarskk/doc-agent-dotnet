using DocAgent.Core.DTOs;
using DocAgent.Core.Models;
using Microsoft.AspNetCore.Http;

namespace DocAgent.Core.Interfaces;

/// <summary>
/// Interface for document service
/// </summary>
public interface IDocumentService
{
    Task<IngestResponseDto> IngestAsync(IngestDocumentDto request);
    Task<List<DocumentDto>> GetAllDocumentsAsync();
    Task<QueryResponseDto> QueryAsync(QueryRequestDto request);
    Task<AskResponseDto> AskAsync(AskRequestDto request);
    Task<bool> DeleteDocumentAsync(string documentId);
    Task<AskResponseDto> AskStreamAsync(AskRequestDto request, HttpResponse response, CancellationToken cancellationToken);
    Task<IngestResponseDto> UpdateDocumentAsync(IngestDocumentDto request);
    Task<SearchResponseDto> SearchAsync(SearchRequestDto request);
    Task<SearchWithSummaryResponseDto> SearchWithSummaryAsync(SearchRequestDto request);
}
