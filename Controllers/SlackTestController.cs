using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Plugins;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Controllers;

/// <summary>
/// Controller for testing Slack integration
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SlackTestController : ControllerBase
{
    private readonly SlackPlugin _slackPlugin;
    private readonly ILogger<SlackTestController> _logger;

    public SlackTestController(SlackPlugin slackPlugin, ILogger<SlackTestController> logger)
    {
        _slackPlugin = slackPlugin;
        _logger = logger;
    }

    /// <summary>
    /// Test Slack integration status and configuration
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetSlackStatus()
    {
        try
        {
            var status = await _slackPlugin.CheckSlackStatusAsync();
            return Ok(new
            {
                Status = "Success",
                Message = status,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Slack status");
            return StatusCode(500, new
            {
                Status = "Error",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Send a test message to Slack
    /// </summary>
    [HttpPost("test-message")]
    public async Task<IActionResult> SendTestMessage([FromBody] TestMessageRequest request)
    {
        try
        {
            var result = await _slackPlugin.SendMessageAsync(
                request.Message ?? "🧪 Test message from AI Code Reviewer",
                request.Channel,
                request.Username
            );

            return Ok(new
            {
                Status = "Success",
                Result = result,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test Slack message");
            return StatusCode(500, new
            {
                Status = "Error",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Send a test code review summary to Slack
    /// </summary>
    [HttpPost("test-review")]
    public async Task<IActionResult> SendTestReview([FromBody] TestReviewRequest request)
    {
        try
        {
            var result = await _slackPlugin.SendCodeReviewSummaryAsync(
                request.PrNumber,
                request.PrTitle ?? "Test Pull Request",
                request.IssuesCount,
                request.Severity ?? "medium",
                request.Channel
            );

            return Ok(new
            {
                Status = "Success",
                Result = result,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test code review summary");
            return StatusCode(500, new
            {
                Status = "Error",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// Request model for test message
/// </summary>
public class TestMessageRequest
{
    public string? Message { get; set; }
    public string? Channel { get; set; }
    public string? Username { get; set; }
}

/// <summary>
/// Request model for test review
/// </summary>
public class TestReviewRequest
{
    public int PrNumber { get; set; } = 123;
    public string? PrTitle { get; set; }
    public int IssuesCount { get; set; } = 2;
    public string? Severity { get; set; }
    public string? Channel { get; set; }
}