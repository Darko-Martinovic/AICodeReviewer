using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Asp.Versioning;
using AICodeReviewer.Services;
using AICodeReviewer.Models.Workflows;

namespace AICodeReviewer.Controllers;

/// <summary>
/// API controller for Semantic Kernel workflow operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[EnableCors("AllowReactApp")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowEngineService _workflowEngineService;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowEngineService workflowEngineService,
        ILogger<WorkflowsController> logger)
    {
        _workflowEngineService = workflowEngineService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available workflow configurations
    /// </summary>
    /// <returns>List of available workflows</returns>
    [HttpGet]
    public async Task<IActionResult> GetAvailableWorkflows()
    {
        try
        {
            var workflows = await _workflowEngineService.GetAvailableWorkflowsAsync();
            return Ok(new
            {
                Count = workflows.Count,
                Workflows = workflows.Select(w => new
                {
                    w.WorkflowName,
                    w.Description,
                    w.Version,
                    StepCount = w.Workflow.Steps.Count,
                    Triggers = w.Triggers.Events
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available workflows");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific workflow configuration
    /// </summary>
    /// <param name="workflowName">The name of the workflow</param>
    /// <returns>Workflow configuration</returns>
    [HttpGet("{workflowName}")]
    public async Task<IActionResult> GetWorkflowConfiguration(string workflowName)
    {
        try
        {
            var workflow = await _workflowEngineService.GetWorkflowConfigurationAsync(workflowName);
            if (workflow == null)
            {
                return NotFound(new { Error = $"Workflow '{workflowName}' not found" });
            }

            return Ok(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workflow configuration for {WorkflowName}", workflowName);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Executes a workflow for a pull request
    /// </summary>
    /// <param name="prNumber">Pull request number</param>
    /// <returns>Workflow execution result</returns>
    [HttpPost("execute/pullrequest/{prNumber:int}")]
    public async Task<IActionResult> ExecutePullRequestWorkflow(int prNumber)
    {
        try
        {
            _logger.LogInformation("🚀 Starting PR workflow execution for PR #{PrNumber}", prNumber);

            var workflowData = new Dictionary<string, object>
            {
                ["prNumber"] = prNumber,
                ["triggerEvent"] = "manual_review",
                ["branch"] = "feature/branch", // Would get this from actual PR data
                ["author"] = "developer" // Would get this from actual PR data
            };

            var context = await _workflowEngineService.ExecuteWorkflowAsync(
                "PullRequestReview", 
                "manual_review", 
                workflowData);

            return Ok(new
            {
                WorkflowId = context.WorkflowId,
                TriggerEvent = context.TriggerEvent,
                StartTime = context.StartTime,
                Duration = DateTime.UtcNow.Subtract(context.StartTime).TotalSeconds,
                StepsExecuted = context.Results.Count,
                Results = context.Results.Select(r => new
                {
                    r.StepId,
                    r.Status,
                    r.ExecutedAt,
                    r.Error,
                    ResultSummary = r.Result?.ToString()?.Substring(0, Math.Min(200, r.Result.ToString()?.Length ?? 0))
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute PR workflow for PR #{PrNumber}", prNumber);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Executes a workflow for a commit
    /// </summary>
    /// <param name="sha">Commit SHA</param>
    /// <returns>Workflow execution result</returns>
    [HttpPost("execute/commit/{sha}")]
    public async Task<IActionResult> ExecuteCommitWorkflow(string sha)
    {
        try
        {
            _logger.LogInformation("🚀 Starting commit workflow execution for commit {CommitSha}", sha);

            var workflowData = new Dictionary<string, object>
            {
                ["commitSha"] = sha,
                ["triggerEvent"] = "manual_review",
                ["branch"] = "main", // Would get this from actual commit data
                ["author"] = "developer" // Would get this from actual commit data
            };

            var context = await _workflowEngineService.ExecuteWorkflowAsync(
                "CommitReview", 
                "manual_review", 
                workflowData);

            return Ok(new
            {
                WorkflowId = context.WorkflowId,
                TriggerEvent = context.TriggerEvent,
                StartTime = context.StartTime,
                Duration = DateTime.UtcNow.Subtract(context.StartTime).TotalSeconds,
                StepsExecuted = context.Results.Count,
                Results = context.Results.Select(r => new
                {
                    r.StepId,
                    r.Status,
                    r.ExecutedAt,
                    r.Error,
                    ResultSummary = r.Result?.ToString()?.Substring(0, Math.Min(200, r.Result.ToString()?.Length ?? 0))
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute commit workflow for commit {CommitSha}", sha);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Executes a custom workflow with provided data
    /// </summary>
    /// <param name="request">Workflow execution request</param>
    /// <returns>Workflow execution result</returns>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteWorkflow([FromBody] WorkflowExecutionRequest request)
    {
        try
        {
            _logger.LogInformation("🚀 Starting custom workflow execution: {WorkflowName}", request.WorkflowName);

            var context = await _workflowEngineService.ExecuteWorkflowAsync(
                request.WorkflowName, 
                request.TriggerEvent, 
                request.Data);

            return Ok(new
            {
                WorkflowId = context.WorkflowId,
                TriggerEvent = context.TriggerEvent,
                StartTime = context.StartTime,
                Duration = DateTime.UtcNow.Subtract(context.StartTime).TotalSeconds,
                StepsExecuted = context.Results.Count,
                Results = context.Results.Select(r => new
                {
                    r.StepId,
                    r.Status,
                    r.ExecutedAt,
                    r.Error,
                    ResultSummary = r.Result?.ToString()?.Substring(0, Math.Min(200, r.Result.ToString()?.Length ?? 0))
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow {WorkflowName}", request.WorkflowName);
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for custom workflow execution
/// </summary>
public class WorkflowExecutionRequest
{
    public string WorkflowName { get; set; } = "";
    public string TriggerEvent { get; set; } = "";
    public Dictionary<string, object> Data { get; set; } = new();
}
