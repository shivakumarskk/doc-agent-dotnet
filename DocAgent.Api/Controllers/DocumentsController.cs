using Microsoft.AspNetCore.Mvc;
using DocAgent.Core.DTOs;
using DocAgent.Core.Interfaces;

namespace DocAgent.Api.Controllers;

/// <summary>
/// API controller for document operations (ingestion, querying, RAG)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all documents
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DocumentDto>>> GetAll()
    {
        try
        {
            var documents = await _documentService.GetAllDocumentsAsync();
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Ingest a document (PDF or text)
    /// </summary>
    [HttpPost("ingest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IngestResponseDto>> Ingest([FromBody] IngestDocumentDto request)
    {
        try
        {
            var response = await _documentService.IngestAsync(request);
            return Ok(response);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "File not found during ingestion");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting document");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing document
    /// </summary>
    [HttpPut("{documentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IngestResponseDto>> Update(string documentId, [FromBody] IngestDocumentDto request)
    {
        try
        {
            var response = await _documentService.UpdateDocumentAsync(request);
            return Ok(response);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "File not found during update");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a document
    /// </summary>
    [HttpDelete("{documentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Delete(string documentId)
    {
        try
        {
            var deleted = await _documentService.DeleteDocumentAsync(documentId);
            if (!deleted)
                return NotFound(new { error = $"Document '{documentId}' not found" });

            return Ok(new { message = $"Document '{documentId}' deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
