using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Services;
using AICodeReviewer.Models;
using Octokit;

namespace AICodeReviewer.Controllers
{
    /// <summary>
    /// API controller for pull request operations
    /// Mirrors the console menu options 4, 5
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PullRequestsController : ControllerBase
    {
        private readonly IGitHubService _gitHubService;
        private readonly ICodeReviewService _codeReviewService;
        private readonly IRepositoryManagementService _repositoryService;
        private readonly IJiraService _jiraService;
        private readonly INotificationService _notificationService;
        private readonly IWorkflowEngineService _workflowEngineService;

        public PullRequestsController(
            IGitHubService gitHubService,
            ICodeReviewService codeReviewService,
            IRepositoryManagementService repositoryService,
            IJiraService jiraService,
            INotificationService notificationService,
            IWorkflowEngineService workflowEngineService
        )
        {
            _gitHubService = gitHubService;
            _codeReviewService = codeReviewService;
            _repositoryService = repositoryService;
            _jiraService = jiraService;
            _notificationService = notificationService;
            _workflowEngineService = workflowEngineService;
        }

        /// <summary>
        /// Gets pull requests with optional state filter
        /// </summary>
        /// <param name="state">Pull request state filter (open, closed, all)</param>
        /// <returns>List of pull requests</returns>
        [HttpGet]
        public async Task<IActionResult> GetPullRequests([FromQuery] string state = "open")
        {
            try
            {
                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();

                // Update GitHubService to use the current repository
                _gitHubService.UpdateRepository(owner, name);

                // Validate repository exists first
                try
                {
                    var repoDetails = await _gitHubService.GetRepositoryDetailsAsync();
                    Console.WriteLine($"✅ Repository validated: {repoDetails.FullName}");
                }
                catch (Exception repoEx)
                {
                    Console.WriteLine($"❌ Repository validation failed: {repoEx.Message}");
                    return NotFound(new
                    {
                        Error = $"Repository '{owner}/{name}' does not exist or is not accessible",
                        Details = repoEx.Message,
                        CurrentRepository = $"{owner}/{name}"
                    });
                }

                // For now, we'll primarily support open PRs as that's what the GitHub service provides
                var pullRequests = await _gitHubService.GetOpenPullRequestsAsync();

                Console.WriteLine($"📋 Found {pullRequests.Count} pull requests in {owner}/{name}");

                return Ok(
                    pullRequests
                        .Select(
                            pr =>
                                new
                                {
                                    Number = pr.Number,
                                    Title = pr.Title,
                                    Author = pr.User.Login,
                                    CreatedAt = pr.CreatedAt,
                                    UpdatedAt = pr.UpdatedAt,
                                    State = pr.State.ToString(),
                                    BaseBranch = pr.Base.Ref,
                                    HeadBranch = pr.Head.Ref,
                                    HtmlUrl = pr.HtmlUrl,
                                    Body = pr.Body ?? "",
                                    IsDraft = pr.Draft
                                }
                        )
                        .ToList()
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Switch repository context for pull request operations
        /// </summary>
        /// <param name="owner">Repository owner</param>
        /// <param name="name">Repository name</param>
        /// <returns>Success response</returns>
        [HttpPost("repository/{owner}/{name}")]
        public async Task<IActionResult> SwitchRepository(string owner, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new { Error = "Owner and name are required" });
                }

                Console.WriteLine($"🔄 Switching repository context to: {owner}/{name}");

                // Set the repository context
                await _repositoryService.SetCurrentRepositoryAsync(owner, name);
                _gitHubService.UpdateRepository(owner, name);

                // Validate the repository is accessible
                try
                {
                    var repoDetails = await _gitHubService.GetRepositoryDetailsAsync();
                    Console.WriteLine($"✅ Repository switch successful: {repoDetails.FullName}");

                    return Ok(new
                    {
                        Message = "Repository context updated successfully",
                        Repository = $"{owner}/{name}",
                        FullName = repoDetails.FullName,
                        Description = repoDetails.Description,
                        IsPrivate = repoDetails.Private
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Repository validation failed: {ex.Message}");
                    return NotFound(new
                    {
                        Error = $"Repository '{owner}/{name}' does not exist or is not accessible",
                        Details = ex.Message
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error switching repository: {ex.Message}");
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Lists open pull requests (Menu option 4)
        /// </summary>
        /// <returns>List of open pull requests</returns>
        [HttpGet("open")]
        public async Task<IActionResult> GetOpenPullRequests()
        {
            try
            {
                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();
                var pullRequests = await _gitHubService.GetOpenPullRequestsAsync();

                return Ok(
                    new
                    {
                        Repository = $"{owner}/{name}",
                        Count = pullRequests.Count,
                        PullRequests = pullRequests.Select(
                            pr =>
                                new
                                {
                                    Number = pr.Number,
                                    Title = pr.Title,
                                    Author = pr.User.Login,
                                    CreatedAt = pr.CreatedAt,
                                    UpdatedAt = pr.UpdatedAt,
                                    State = pr.State.ToString(),
                                    BaseBranch = pr.Base.Ref,
                                    HeadBranch = pr.Head.Ref,
                                    Url = pr.HtmlUrl,
                                    IsDraft = pr.Draft
                                }
                        )
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Reviews a specific pull request (Menu option 5)
        /// </summary>
        /// <param name="number">Pull request number</param>
        /// <returns>AI review of the pull request</returns>
        [HttpPost("review/{number:int}")]
        public async Task<IActionResult> ReviewPullRequest(int number)
        {
            try
            {
                if (number <= 0)
                {
                    return BadRequest(new { Error = "Invalid pull request number" });
                }

                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();

                Console.WriteLine($"🔍 DEBUGGING INFO:");
                Console.WriteLine($"   - Current Repository: {owner}/{name}");
                Console.WriteLine($"   - Pull Request Number: {number}");
                Console.WriteLine($"   - GitHub API Target: repos/{owner}/{name}/pulls/{number}");

                // Update GitHubService to ensure it uses the current repository
                _gitHubService.UpdateRepository(owner, name);

                // STEP 1: Validate repository exists and is accessible
                try
                {
                    var repoDetails = await _gitHubService.GetRepositoryDetailsAsync();
                    Console.WriteLine($"✅ Repository validation: {repoDetails.FullName} is accessible");
                }
                catch (Exception repoEx)
                {
                    Console.WriteLine($"❌ Repository validation failed: {repoEx.Message}");
                    return NotFound(new
                    {
                        Error = $"Repository '{owner}/{name}' does not exist or is not accessible",
                        Details = repoEx.Message
                    });
                }

                // STEP 2: Validate pull request exists
                PullRequest pullRequest;
                try
                {
                    pullRequest = await _gitHubService.GetPullRequestAsync(number);
                    Console.WriteLine($"✅ Pull request validation: PR #{number} exists");
                    Console.WriteLine($"   - Title: {pullRequest.Title}");
                    Console.WriteLine($"   - State: {pullRequest.State}");
                    Console.WriteLine($"   - Head: {pullRequest.Head.Ref}");
                    Console.WriteLine($"   - Base: {pullRequest.Base.Ref}");
                }
                catch (Exception prEx)
                {
                    Console.WriteLine($"❌ Pull request validation failed: {prEx.Message}");
                    return NotFound(new
                    {
                        Error = $"Pull request #{number} does not exist in repository '{owner}/{name}'",
                        Details = prEx.Message
                    });
                }

                // STEP 3: Get and validate PR files
                var prFiles = await _gitHubService.GetPullRequestFilesAsync(number);
                Console.WriteLine($"📋 Pull request files: {prFiles.Count} total files changed");

                if (!prFiles.Any())
                {
                    return BadRequest(new
                    {
                        Error = $"Pull request #{number} has no changed files to review",
                        PullRequest = new
                        {
                            Number = pullRequest.Number,
                            Title = pullRequest.Title,
                            State = pullRequest.State.ToString(),
                            FilesChanged = 0
                        }
                    });
                }

                // Log file details for debugging
                foreach (var file in prFiles)
                {
                    Console.WriteLine($"   • {file.FileName} ({file.Status}) - {file.Changes} changes");
                }

                // Execute the full workflow instead of just code review
                Console.WriteLine($"🚀 Starting PR review workflow for PR #{number}");
                Console.WriteLine($"📊 Repository: {owner}/{name}");

                var workflowData = new Dictionary<string, object>
                {
                    ["pullRequestNumber"] = number,
                    ["owner"] = owner,
                    ["repository"] = name,
                    ["prNumber"] = number, // Add alternative key for plugins
                    ["content"] = "AI Code Review completed", // Add content for GitHub comment
                    ["project"] = "OPS", // Use correct Jira project key from .env
                    ["issueType"] = "Task", // Use correct issue type for Jira
                    ["summary"] = $"Code Review Issues in PR #{number}",
                    ["description"] = "Issues found during AI code review",
                    ["priority"] = "High"
                };

                Console.WriteLine(
                    $"📋 Workflow data prepared: {string.Join(", ", workflowData.Keys)}"
                );

                var workflowContext = await _workflowEngineService.ExecuteWorkflowAsync(
                    "PullRequestReview",
                    "manual_review",
                    workflowData
                );

                Console.WriteLine($"🔄 Workflow execution completed");
                Console.WriteLine($"📈 Steps executed: {workflowContext.Results.Count}");
                foreach (var result in workflowContext.Results)
                {
                    Console.WriteLine($"  ▸ {result.StepId}: {result.Status} - {result.Result}");
                    if (!string.IsNullOrEmpty(result.Error))
                    {
                        Console.WriteLine($"    ❌ Error: {result.Error}");
                    }
                }

                // Extract the review result from workflow context
                CodeReviewResult review;
                try
                {
                    Console.WriteLine($"🔍 Attempting to review PR #{number}");
                    review = await _codeReviewService.ReviewPullRequestAsync(number);
                    Console.WriteLine($"✅ Successfully retrieved real review for PR #{number}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error reviewing PR #{number}: {ex.Message}");
                    Console.WriteLine($"🔍 Exception type: {ex.GetType().Name}");

                    return BadRequest(new
                    {
                        Error = $"Failed to review pull request #{number}",
                        Details = ex.Message,
                        Repository = $"{owner}/{name}"
                    });
                }

                // Map CodeReviewResult to the format expected by frontend
                var mappedReview = new
                {
                    // Add debugging information to the response
                    debugInfo = new
                    {
                        repository = $"{owner}/{name}",
                        pullRequestNumber = number,
                        reviewedAt = DateTime.UtcNow,
                        githubApiUrl = $"https://api.github.com/repos/{owner}/{name}/pulls/{number}",
                        filesReviewed = review.ReviewedFiles?.Count ?? 0,
                        totalIssuesFound = review.DetailedIssues?.Count ?? 0
                    },
                    summary = GenerateSummary(review, owner, name, number),
                    issues = review.DetailedIssues?.Select(
                        issue => (object)new
                        {
                            severity = issue.Severity,
                            file = issue.FileName,
                            line = issue.LineNumber ?? 0,
                            message = issue.Description,
                            suggestion = issue.Recommendation
                        }
                    ).ToList() ?? new List<object>(),
                    suggestions = ExtractSuggestions(review),
                    complexity = DetermineComplexity(review),
                    testCoverage = DetermineTestCoverage(review),
                    security = review.DetailedIssues?.Where(issue => issue.Category?.ToLower().Contains("security") == true)
                        .Select(issue => (object)new
                        {
                            severity = issue.Severity,
                            type = issue.Category,
                            description = issue.Description,
                            recommendation = issue.Recommendation
                        }).ToList() ?? new List<object>()
                };

                Console.WriteLine($"🔍 Backend PR review workflow completed:");
                Console.WriteLine($"  - Original issues count: {review.DetailedIssues?.Count ?? 0}");
                Console.WriteLine($"  - Mapped issues count: {mappedReview.issues.Count}");
                Console.WriteLine($"  - Summary: {mappedReview.summary}");
                Console.WriteLine($"  - Workflow steps executed: {workflowContext.Results.Count}");

                return Ok(mappedReview);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ReviewPullRequest: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets detailed information about a specific pull request
        /// </summary>
        /// <param name="number">Pull request number</param>
        /// <returns>Detailed pull request information</returns>
        [HttpGet("{number:int}")]
        public async Task<IActionResult> GetPullRequest(int number)
        {
            try
            {
                if (number <= 0)
                {
                    return BadRequest(new { Error = "Invalid pull request number" });
                }

                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();
                var pullRequest = await _gitHubService.GetPullRequestAsync(number);

                return Ok(
                    new
                    {
                        Repository = $"{owner}/{name}",
                        PullRequest = new
                        {
                            Number = pullRequest.Number,
                            Title = pullRequest.Title,
                            Body = pullRequest.Body,
                            Author = pullRequest.User.Login,
                            CreatedAt = pullRequest.CreatedAt,
                            UpdatedAt = pullRequest.UpdatedAt,
                            State = pullRequest.State.ToString(),
                            BaseBranch = pullRequest.Base.Ref,
                            HeadBranch = pullRequest.Head.Ref,
                            Url = pullRequest.HtmlUrl,
                            IsDraft = pullRequest.Draft,
                            Mergeable = pullRequest.Mergeable,
                            Stats = new
                            {
                                Additions = pullRequest.Additions,
                                Deletions = pullRequest.Deletions,
                                ChangedFiles = pullRequest.ChangedFiles
                            }
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the files changed in a pull request
        /// </summary>
        /// <param name="number">Pull request number</param>
        /// <returns>List of changed files</returns>
        [HttpGet("{number:int}/files")]
        public async Task<IActionResult> GetPullRequestFiles(int number)
        {
            try
            {
                if (number <= 0)
                {
                    return BadRequest(new { Error = "Invalid pull request number" });
                }

                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();
                var files = await _gitHubService.GetPullRequestFilesAsync(number);

                return Ok(
                    new
                    {
                        Repository = $"{owner}/{name}",
                        PullRequestNumber = number,
                        Files = files.Select(
                            f =>
                                new
                                {
                                    Filename = f.FileName, // Note: Octokit uses FileName, not Filename
                                    Status = f.Status,
                                    Additions = f.Additions,
                                    Deletions = f.Deletions,
                                    Changes = f.Changes,
                                    BlobUrl = f.BlobUrl,
                                    Patch = f.Patch
                                }
                        )
                    }
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        private string GenerateSummary(CodeReviewResult review, string owner, string repo, int prNumber)
        {
            // Generate summary with repository and PR context
            var baseMessage = $"Review for {owner}/{repo} PR #{prNumber}: ";

            if (review.HasIssues)
            {
                return baseMessage + $"Found {review.DetailedIssues?.Count ?? 0} issue(s) in {review.Metrics.FilesReviewed} file(s).";
            }
            else
            {
                return baseMessage + $"No issues found in {review.Metrics.FilesReviewed} file(s).";
            }
        }

        private List<string> ExtractSuggestions(CodeReviewResult review)
        {
            var suggestions = new List<string>();

            // Extract actual suggestions from detailed issues only
            suggestions.AddRange(
                review.DetailedIssues
                    .Where(i => !string.IsNullOrWhiteSpace(i.Recommendation))
                    .Select(i => i.Recommendation)
                    .Distinct()
            );

            return suggestions.Take(10).ToList();
        }

        private string DetermineComplexity(CodeReviewResult review)
        {
            // Return actual complexity from review data if available
            var issueCount = review.IssueCount;
            var filesReviewed = review.Metrics.FilesReviewed;

            if (issueCount == 0)
                return "Low";

            var issuesPerFile = filesReviewed > 0 ? (double)issueCount / filesReviewed : issueCount;

            if (issuesPerFile >= 3)
                return "High";
            if (issuesPerFile >= 1.5)
                return "Medium";
            return "Low";
        }

        private string DetermineTestCoverage(CodeReviewResult review)
        {
            // Check if test coverage data is available from the actual review
            var hasTestFiles = review.ReviewedFiles.Any(
                f => f.ToLower().Contains("test") || f.ToLower().Contains("spec")
            );

            if (hasTestFiles)
            {
                return "Test files detected";
            }

            return "No test coverage data available";
        }
    }
}
