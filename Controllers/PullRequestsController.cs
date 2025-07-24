using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Services.Interfaces;

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

                return Ok(new
                {
                    Repository = $"{owner}/{name}",
                    PullRequestNumber = number,
                    Review = review
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
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
    }
}
