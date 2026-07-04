using Microsoft.AspNetCore.Mvc;
using DocAgent.Core.DTOs;
using DocAgent.Core.Interfaces;

namespace DocAgent.Api.Controllers;

/// <summary>
/// API controller for LLM operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LlmController : ControllerBase
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<LlmController> _logger;

    public LlmController(ILlmClient llmClient, ILogger<LlmController> logger)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate text using LLM
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> Generate([FromBody] GenerateRequestDto request)
    {
        try
        {
            var response = await _llmClient.GenerateAsync(request.Model, request.Prompt, request.MaxTokens);
            return Ok(new { response });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid generate request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
