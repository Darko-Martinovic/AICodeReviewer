using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Models;

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

        public PullRequestsController(
            IGitHubService gitHubService,
            ICodeReviewService codeReviewService,
            IRepositoryManagementService repositoryService)
        {
            _gitHubService = gitHubService;
            _codeReviewService = codeReviewService;
            _repositoryService = repositoryService;
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

                // For now, we'll primarily support open PRs as that's what the GitHub service provides
                var pullRequests = await _gitHubService.GetOpenPullRequestsAsync();

                return Ok(pullRequests.Select(pr => new
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
                }).ToList());
            }
            catch (Exception ex)
            {
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

                return Ok(new
                {
                    Repository = $"{owner}/{name}",
                    Count = pullRequests.Count,
                    PullRequests = pullRequests.Select(pr => new
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
                    })
                });
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
                var review = await _codeReviewService.ReviewPullRequestAsync(number);

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
                        }).ToList()
                };

                Console.WriteLine($"🔍 Backend PR review mapping:");
                Console.WriteLine($"  - Original issues count: {review.DetailedIssues.Count}");
                Console.WriteLine($"  - Mapped issues count: {mappedReview.issues.Count}");
                Console.WriteLine($"  - Summary: {mappedReview.summary}");

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

                return Ok(new
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
                });
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

                return Ok(new
                {
                    Repository = $"{owner}/{name}",
                    PullRequestNumber = number,
                    Files = files.Select(f => new
                    {
                        Filename = f.FileName, // Note: Octokit uses FileName, not Filename
                        Status = f.Status,
                        Additions = f.Additions,
                        Deletions = f.Deletions,
                        Changes = f.Changes,
                        BlobUrl = f.BlobUrl,
                        Patch = f.Patch
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
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

                return $"Pull request review completed. Found {summaryParts.Count} categories of issues: {string.Join(", ", summaryParts)}. " +
                       $"Total files reviewed: {review.Metrics.FilesReviewed}. " +
                       $"Review took {review.Metrics.Duration.TotalSeconds:F1} seconds.";
            }
            else
            {
                return $"Pull request review completed successfully. No issues found in {review.Metrics.FilesReviewed} file(s). " +
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
    }
}
