using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Asp.Versioning;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Models;
using AICodeReviewer.Services;
using System.Text.Json;

namespace AICodeReviewer.Controllers
{
    /// <summary>
    /// API controller for commit-related operations
    /// Mirrors the console menu options 1, 2, 3
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    [EnableCors("AllowReactApp")]
    public class CommitsController : ControllerBase
    {
        private readonly IGitHubService _gitHubService;
        private readonly ICodeReviewService _codeReviewService;
        private readonly IRepositoryManagementService _repositoryService;
        private readonly IWorkflowEngineService _workflowEngineService;

        public CommitsController(
            IGitHubService gitHubService,
            ICodeReviewService codeReviewService,
            IRepositoryManagementService repositoryService,
            IWorkflowEngineService workflowEngineService)
        {
            _gitHubService = gitHubService;
            _codeReviewService = codeReviewService;
            _repositoryService = repositoryService;
            _workflowEngineService = workflowEngineService;
        }

        /// <summary>
        /// Gets all repository branches
        /// </summary>
        /// <returns>List of branch names</returns>
        [HttpGet("branches")]
        public async Task<IActionResult> GetBranches()
        {
            try
            {
                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();
                _gitHubService.UpdateRepository(owner, name);
                var branches = await _gitHubService.GetBranchesAsync();

                return Ok(new
                {
                    Repository = $"{owner}/{name}",
                    Branches = branches
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Lists recent commits (Menu option 1)
        /// </summary>
        /// <param name="count">Number of commits to retrieve (default: 10)</param>
        /// <param name="branch">Branch name to get commits from (optional, uses default branch if not specified)</param>
        /// <returns>List of recent commits</returns>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentCommits([FromQuery] int count = 10, [FromQuery] string? branch = null)
        {
            try
            {
                Console.WriteLine($"📝 GetRecentCommits called: count={count}, branch={branch ?? "null (will use default)"}");
                
                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();
                Console.WriteLine($"📝 Current repository: {owner}/{name}");
                
                _gitHubService.UpdateRepository(owner, name);
                Console.WriteLine($"📝 Getting commits from GitHub API...");
                
                var commits = await _gitHubService.GetRecentCommitsAsync(count, branch);
                Console.WriteLine($"📝 Retrieved {commits.Count} commits successfully");

                // Fetch detailed commit info to get file counts (done in parallel for performance)
                var commitDetailsTask = commits.Select(async c =>
                {
                    try
                    {
                        var details = await _gitHubService.GetCommitDetailAsync(c.Sha);
                        return new
                        {
                            sha = c.Sha,
                            message = c.Commit.Message,
                            author = c.Commit.Author.Name,
                            authorEmail = c.Commit.Author.Email ?? "",
                            date = c.Commit.Author.Date,
                            htmlUrl = c.HtmlUrl,
                            filesChanged = details.Files?.Count ?? 0
                        };
                    }
                    catch
                    {
                        // If fetching details fails, return commit without file count
                        return new
                        {
                            sha = c.Sha,
                            message = c.Commit.Message,
                            author = c.Commit.Author.Name,
                            authorEmail = c.Commit.Author.Email ?? "",
                            date = c.Commit.Author.Date,
                            htmlUrl = c.HtmlUrl,
                            filesChanged = 0
                        };
                    }
                }).ToList();

                var commitDetails = await Task.WhenAll(commitDetailsTask);

                return Ok(new
                {
                    Repository = $"{owner}/{name}",
                    Branch = branch ?? "default",
                    Count = commits.Count,
                    Commits = commitDetails
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetRecentCommits: {ex.Message}");
                Console.WriteLine($"❌ Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ Inner Exception: {ex.InnerException.Message}");
                }
                
                return BadRequest(new { 
                    Error = ex.Message,
                    Type = ex.GetType().Name,
                    Details = "Check backend console for full stack trace"
                });
            }
        }

        /// <summary>
        /// Reviews the latest commit (Menu option 2)
        /// </summary>
        /// <returns>AI review of the latest commit</returns>
        [HttpPost("review-latest")]
        public async Task<IActionResult> ReviewLatestCommit()
        {
            try
            {
                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();
                var commits = await _gitHubService.GetRecentCommitsAsync(1);

                if (!commits.Any())
                {
                    return NotFound(new { Error = "No commits found in repository" });
                }

                var latestCommit = commits.First();
                var review = await _codeReviewService.ReviewCommitAsync(latestCommit.Sha);

                return Ok(new
                {
                    Repository = $"{owner}/{name}",
                    Commit = new
                    {
                        Sha = latestCommit.Sha,
                        Message = latestCommit.Commit.Message,
                        Author = latestCommit.Commit.Author.Name,
                        Date = latestCommit.Commit.Author.Date
                    },
                    Review = review
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Reviews a specific commit by hash (Menu option 3)
        /// </summary>
        /// <param name="sha">Commit SHA hash</param>
        /// <returns>AI review of the specified commit</returns>
        [HttpPost("review/{sha}")]
        public async Task<IActionResult> ReviewCommitByHash(string sha)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sha))
                {
                    return BadRequest(new { Error = "Commit SHA is required" });
                }

                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();

                // Get commit details first
                var commit = await _gitHubService.GetCommitAsync(sha);
                var review = await _codeReviewService.ReviewCommitAsync(sha);

                // Map CodeReviewResult to the format expected by frontend
                var mappedReview = new
                {
                    summary = GenerateSummary(review),
                    issues = review.DetailedIssues.Select(issue => new
                    {
                        severity = issue.Severity,
                        file = issue.FileName,
                        line = issue.LineNumber ?? 0,
                        message = issue.Description,
                        suggestion = issue.Recommendation
                    }).ToList(),
                    suggestions = ExtractSuggestions(review),
                    complexity = DetermineComplexity(review),
                    testCoverage = DetermineTestCoverage(review),
                    security = review.DetailedIssues
                        .Where(issue => issue.Category?.ToLower().Contains("security") == true)
                        .Select(issue => new
                        {
                            severity = issue.Severity,
                            type = issue.Category,
                            description = issue.Description,
                            recommendation = issue.Recommendation
                        }).ToList(),
                    tokensUsed = review.Metrics.TokensUsed,
                    estimatedCost = review.Metrics.EstimatedCost
                };

                Console.WriteLine($"🔍 Backend review mapping:");
                Console.WriteLine($"  - Original issues count: {review.DetailedIssues.Count}");
                Console.WriteLine($"  - Mapped issues count: {mappedReview.issues.Count}");
                Console.WriteLine($"  - Summary: {mappedReview.summary}");
                Console.WriteLine($"  - Suggestions count: {mappedReview.suggestions.Count}");
                Console.WriteLine($"  - Security issues count: {mappedReview.security.Count}");
                Console.WriteLine($"  - Tokens Used: {mappedReview.tokensUsed}");
                Console.WriteLine($"  - Estimated Cost: ${mappedReview.estimatedCost:F4}");

                return Ok(mappedReview);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ReviewCommitByHash: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        private string GenerateSummary(CodeReviewResult review)
        {
            if (review.HasIssues)
            {
                var highSeverityCount = review.DetailedIssues.Count(i => i.Severity?.ToLower() == "high" || i.Severity?.ToLower() == "critical");
                var mediumSeverityCount = review.DetailedIssues.Count(i => i.Severity?.ToLower() == "medium");
                var lowSeverityCount = review.DetailedIssues.Count(i => i.Severity?.ToLower() == "low");

                var summaryParts = new List<string>();
                if (highSeverityCount > 0) summaryParts.Add($"{highSeverityCount} high-priority issue(s)");
                if (mediumSeverityCount > 0) summaryParts.Add($"{mediumSeverityCount} medium-priority issue(s)");
                if (lowSeverityCount > 0) summaryParts.Add($"{lowSeverityCount} low-priority issue(s)");

                return $"Code review completed. Found {summaryParts.Count} categories of issues: {string.Join(", ", summaryParts)}. " +
                       $"Total files reviewed: {review.Metrics.FilesReviewed}. " +
                       $"Review took {review.Metrics.Duration.TotalSeconds:F1} seconds.";
            }
            else
            {
                return $"Code review completed successfully. No issues found in {review.Metrics.FilesReviewed} file(s). " +
                       $"Review took {review.Metrics.Duration.TotalSeconds:F1} seconds. Great work!";
            }
        }

        private List<string> ExtractSuggestions(CodeReviewResult review)
        {
            var suggestions = new List<string>();

            // Add general suggestions based on the review
            if (review.DetailedIssues.Any(i => i.Category?.ToLower().Contains("performance") == true))
            {
                suggestions.Add("Consider performance optimizations in identified areas");
            }

            if (review.DetailedIssues.Any(i => i.Category?.ToLower().Contains("security") == true))
            {
                suggestions.Add("Review and implement security best practices");
            }

            if (review.DetailedIssues.Any(i => i.Category?.ToLower().Contains("maintainability") == true))
            {
                suggestions.Add("Improve code maintainability and readability");
            }

            // Add specific recommendations from detailed issues
            suggestions.AddRange(review.DetailedIssues
                .Where(i => !string.IsNullOrWhiteSpace(i.Recommendation))
                .Select(i => i.Recommendation)
                .Distinct()
                .Take(5)); // Limit to 5 suggestions to avoid overwhelming

            // If no specific suggestions, add general ones
            if (!suggestions.Any())
            {
                if (review.HasIssues)
                {
                    suggestions.Add("Address the identified issues to improve code quality");
                    suggestions.Add("Consider adding unit tests for better coverage");
                }
                else
                {
                    suggestions.Add("Code looks good! Consider adding documentation if needed");
                    suggestions.Add("Keep up the good coding practices");
                }
            }

            return suggestions.Take(10).ToList(); // Limit to 10 suggestions maximum
        }

        private string DetermineComplexity(CodeReviewResult review)
        {
            // Simple heuristic based on number of issues and files
            var issueCount = review.IssueCount;
            var filesReviewed = review.Metrics.FilesReviewed;

            if (issueCount == 0) return "Low";

            var issuesPerFile = filesReviewed > 0 ? (double)issueCount / filesReviewed : issueCount;

            if (issuesPerFile >= 3) return "High";
            if (issuesPerFile >= 1.5) return "Medium";
            return "Low";
        }

        private string DetermineTestCoverage(CodeReviewResult review)
        {
            // Simple heuristic - look for test files or test-related issues
            var hasTestFiles = review.ReviewedFiles.Any(f =>
                f.ToLower().Contains("test") ||
                f.ToLower().Contains("spec") ||
                f.ToLower().EndsWith(".test.cs") ||
                f.ToLower().EndsWith(".test.js"));

            if (hasTestFiles)
            {
                return "Some test coverage detected";
            }

            var hasTestRelatedIssues = review.DetailedIssues.Any(i =>
                i.Description?.ToLower().Contains("test") == true ||
                i.Recommendation?.ToLower().Contains("test") == true);

            if (hasTestRelatedIssues)
            {
                return "Test coverage needs improvement";
            }

            return "Test coverage analysis not available";
        }

        /// <summary>
        /// Gets detailed information about a specific commit
        /// </summary>
        /// <param name="sha">Commit SHA hash</param>
        /// <returns>Detailed commit information</returns>
        [HttpGet("{sha}")]
        public async Task<IActionResult> GetCommit(string sha)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sha))
                {
                    return BadRequest(new { Error = "Commit SHA is required" });
                }

                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();
                var commit = await _gitHubService.GetCommitAsync(sha);

                return Ok(new
                {
                    Repository = $"{owner}/{name}",
                    Commit = new
                    {
                        Sha = commit.Sha,
                        Message = commit.Commit.Message,
                        Author = commit.Commit.Author.Name,
                        Date = commit.Commit.Author.Date,
                        Url = commit.HtmlUrl,
                        Stats = new
                        {
                            Additions = commit.Stats.Additions,
                            Deletions = commit.Stats.Deletions,
                            Total = commit.Stats.Total
                        },
                        Files = commit.Files.Select(f => new
                        {
                            Filename = f.Filename,
                            Status = f.Status,
                            Additions = f.Additions,
                            Deletions = f.Deletions,
                            Changes = f.Changes
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Reviews a commit using Semantic Kernel workflow (New workflow-based approach)
        /// </summary>
        /// <param name="sha">Commit SHA hash</param>
        /// <returns>Workflow execution result</returns>
        [HttpPost("review-workflow/{sha}")]
        public async Task<IActionResult> ReviewCommitWithWorkflow(string sha)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sha))
                {
                    return BadRequest(new { Error = "Commit SHA is required" });
                }

                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();

                // Get commit details for context
                var commit = await _gitHubService.GetCommitAsync(sha);

                // Execute the workflow (for notifications, etc.)
                var workflowData = new Dictionary<string, object>
                {
                    ["commitSha"] = sha,
                    ["repository"] = $"{owner}/{name}",
                    ["author"] = commit.Commit.Author.Name,
                    ["branch"] = "main", // Could be extracted from commit context
                    ["triggerEvent"] = "manual_review"
                };

                Console.WriteLine($"🚀 Executing commit workflow for {sha}");
                var workflowContext = await _workflowEngineService.ExecuteWorkflowAsync(
                    "CommitReview",
                    "manual_review",
                    workflowData);

                // Get the actual review using the service directly (same as PR approach)
                CodeReviewResult review;
                try
                {
                    Console.WriteLine($"🔍 Getting actual commit review for {sha}");
                    review = await _codeReviewService.ReviewCommitAsync(sha);
                    Console.WriteLine($"✅ Successfully retrieved commit review for {sha}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error reviewing commit {sha}: {ex.Message}");
                    return BadRequest(new
                    {
                        Error = $"Failed to review commit {sha}",
                        Details = ex.Message,
                        Repository = $"{owner}/{name}"
                    });
                }

                // Transform the review result to frontend format (same as regular endpoint)
                var transformedReviewData = new
                {
                    summary = GenerateSummary(review),
                    issues = review.DetailedIssues.Select(issue => new
                    {
                        severity = issue.Severity,
                        file = issue.FileName,
                        line = issue.LineNumber ?? 0,
                        message = issue.Description,
                        suggestion = issue.Recommendation
                    }).ToList(),
                    suggestions = ExtractSuggestions(review),
                    complexity = DetermineComplexity(review),
                    testCoverage = DetermineTestCoverage(review),
                    security = review.DetailedIssues
                        .Where(issue => issue.Category?.ToLower().Contains("security") == true)
                        .Select(issue => new
                        {
                            severity = issue.Severity,
                            type = issue.Category,
                            description = issue.Description,
                            recommendation = issue.Recommendation
                        }).ToList(),
                    tokensUsed = review.Metrics.TokensUsed,
                    estimatedCost = review.Metrics.EstimatedCost
                };

                Console.WriteLine($"🔍 Commit workflow review mapping:");
                Console.WriteLine($"  - Issues count: {transformedReviewData.issues.Count}");
                Console.WriteLine($"  - Summary: {transformedReviewData.summary}");
                Console.WriteLine($"  - Suggestions count: {transformedReviewData.suggestions.Count}");
                Console.WriteLine($"  - Security issues count: {transformedReviewData.security.Count}");
                Console.WriteLine($"  - Tokens Used: {transformedReviewData.tokensUsed}");
                Console.WriteLine($"  - Estimated Cost: ${transformedReviewData.estimatedCost:F4}");
                Console.WriteLine($"  - Workflow steps executed: {workflowContext.Results.Count}");

                return Ok(new
                {
                    WorkflowExecution = new
                    {
                        WorkflowId = workflowContext.WorkflowId,
                        TriggerEvent = workflowContext.TriggerEvent,
                        StartTime = workflowContext.StartTime,
                        Duration = DateTime.UtcNow.Subtract(workflowContext.StartTime).TotalSeconds,
                        StepsExecuted = workflowContext.Results.Count
                    },
                    Commit = new
                    {
                        Sha = commit.Sha,
                        Message = commit.Commit.Message,
                        Author = commit.Commit.Author.Name,
                        Date = commit.Commit.Author.Date
                    },
                    Review = transformedReviewData,
                    WorkflowResults = workflowContext.Results.Select(r => new
                    {
                        r.StepId,
                        r.Status,
                        r.ExecutedAt,
                        r.Error,
                        HasResult = r.Result != null
                    })
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Workflow execution failed: {ex.Message}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the content of a specific file from a commit
        /// </summary>
        /// <param name="sha">Commit SHA hash</param>
        /// <param name="filename">Path to the file</param>
        /// <returns>File content</returns>
        [HttpGet("{sha}/file")]
        public async Task<IActionResult> GetCommitFileContent(string sha, [FromQuery] string filename)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sha))
                {
                    return BadRequest(new { Error = "Commit SHA is required" });
                }

                if (string.IsNullOrWhiteSpace(filename))
                {
                    return BadRequest(new { Error = "Filename is required" });
                }

                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();
                _gitHubService.UpdateRepository(owner, name);

                // Get the commit to ensure it exists and get the file content at that commit
                var commit = await _gitHubService.GetCommitAsync(sha);
                var content = await _gitHubService.GetFileContentFromBranchAsync(filename, sha);

                return Ok(new
                {
                    Repository = $"{owner}/{name}",
                    CommitSha = sha,
                    Filename = filename,
                    Content = content
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
