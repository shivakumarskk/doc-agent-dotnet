using Microsoft.AspNetCore.Mvc;
using DocAgent.Core.DTOs;
using DocAgent.Core.Interfaces;

namespace DocAgent.Api.Controllers;

/// <summary>
/// API controller for skill operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _skillService;
    private readonly ILogger<SkillsController> _logger;

    public SkillsController(ISkillService skillService, ILogger<SkillsController> logger)
    {
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invoke a skill
    /// </summary>
    [HttpPost("invoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> Invoke([FromBody] InvokeSkillDto request)
    {
        try
        {
            var result = await _skillService.InvokeAsync(request.SkillName, request.Input);
            return Ok(new { response = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Skill not found");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking skill");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}

/// <summary>
/// DTO for skill invocation
/// </summary>
public class InvokeSkillDto
{
    public string SkillName { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
}
