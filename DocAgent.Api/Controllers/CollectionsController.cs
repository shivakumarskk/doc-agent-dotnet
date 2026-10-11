using System.Net;
using DocAgent.Core.DTOs;
using DocAgent.Core.Interfaces;
using DocAgent.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocAgent.Api.Controllers;

/// <summary>
/// API controller for Chroma collection and database administration.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionAdminService _collectionAdminService;
    private readonly ILogger<CollectionsController> _logger;

    public CollectionsController(ICollectionAdminService collectionAdminService, ILogger<CollectionsController> logger)
    {
        _collectionAdminService = collectionAdminService ?? throw new ArgumentNullException(nameof(collectionAdminService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<VectorCollectionInfo>>> List([FromQuery] int? limit, [FromQuery] int? offset)
    {
        try
        {
            var collections = await _collectionAdminService.ListCollectionsAsync(limit, offset);
            return Ok(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing collections");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpGet("count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetCollectionCount()
    {
        try
        {
            var count = await _collectionAdminService.GetCollectionCountAsync();
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection count");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpGet("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VectorCollectionInfo>> Get(string name)
    {
        try
        {
            var collection = await _collectionAdminService.GetCollectionAsync(name);
            return Ok(collection);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid collection get request");
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Collection not found");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VectorCollectionInfo>> Create([FromBody] CollectionCreateRequestDto request)
    {
        try
        {
            var collection = await _collectionAdminService.CreateCollectionAsync(request);
            return Ok(collection);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid collection create request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpPost("get-or-create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VectorCollectionInfo>> GetOrCreate([FromBody] CollectionCreateRequestDto request)
    {
        try
        {
            var collection = await _collectionAdminService.GetOrCreateCollectionAsync(request);
            return Ok(collection);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid get-or-create request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in get-or-create collection");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpPut("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VectorCollectionInfo>> Update(string name, [FromBody] CollectionUpdateRequestDto request)
    {
        try
        {
            var collection = await _collectionAdminService.UpdateCollectionAsync(name, request);
            return Ok(collection);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update collection request");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Blocked update collection operation");
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Collection not found for update");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpDelete("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Delete(string name)
    {
        try
        {
            await _collectionAdminService.DeleteCollectionAsync(name);
            return Ok(new { message = $"Collection '{name}' deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid delete collection request");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Blocked delete collection operation");
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Collection not found for delete");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpGet("{name}/records/count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetRecordCount(string name)
    {
        try
        {
            var count = await _collectionAdminService.GetRecordCountAsync(name);
            return Ok(new { collection = name, count });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid count records request");
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Collection not found for records count");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting record count");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpPost("{name}/records/upsert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpsertRecords(string name, [FromBody] CollectionUpsertRequestDto request)
    {
        try
        {
            await _collectionAdminService.UpsertRecordsAsync(name, request);
            return Ok(new { message = $"Upsert completed for collection '{name}'", total = request.Ids.Length });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid upsert records request");
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Collection not found for upsert");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting records");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpPost("{name}/records/delete-by-filter")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteByFilter(string name, [FromBody] CollectionDeleteByFilterRequestDto request)
    {
        try
        {
            await _collectionAdminService.DeleteRecordsByFilterAsync(name, request);
            return Ok(new { message = $"Delete-by-filter completed for collection '{name}'" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid delete-by-filter request");
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Collection not found for delete-by-filter");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting records by filter");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpGet("databases/{name}/exists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DatabaseExistsResponseDto>> DatabaseExists(string name)
    {
        try
        {
            var response = await _collectionAdminService.DatabaseExistsAsync(name);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid database exists request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database existence");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpPost("databases")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateDatabase([FromBody] DatabaseCreateRequestDto request)
    {
        try
        {
            await _collectionAdminService.CreateDatabaseAsync(request);
            return Ok(new { message = $"Database '{request.Name}' created successfully" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid create database request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpGet("diagnostics/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CollectionDiagnosticsDto>> GetHealth()
    {
        try
        {
            var diagnostics = await _collectionAdminService.GetDiagnosticsAsync();
            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Chroma diagnostics health");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}