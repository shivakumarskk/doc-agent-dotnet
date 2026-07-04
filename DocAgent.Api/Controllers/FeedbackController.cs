using Microsoft.AspNetCore.Mvc;
using DocAgent.Core.DTOs;
using DocAgent.Core.Interfaces;
using DocAgent.Core.Models;

namespace DocAgent.Api.Controllers;

/// <summary>
/// API controller for feedback operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(IFeedbackRepository feedbackRepository, ILogger<FeedbackController> logger)
    {
        _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submit feedback for a query
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<object> Submit([FromBody] FeedbackDto request)
    {
        try
        {
            var feedback = new Feedback
            {
                QueryId = request.QueryId,
                ContextChunkIds = request.ContextChunkIds,
                IsPositive = request.IsPositive,
                Comment = request.Comment
            };

            _feedbackRepository.Add(feedback);

            return Ok(new { status = "saved", feedbackId = feedback.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all feedback
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<object> GetAll()
    {
        try
        {
            var feedback = _feedbackRepository.GetAll();
            return Ok(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get feedback for a specific query
    /// </summary>
    [HttpGet("{queryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<object> GetByQueryId(string queryId)
    {
        try
        {
            var feedback = _feedbackRepository.GetByQueryId(queryId);
            return Ok(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
