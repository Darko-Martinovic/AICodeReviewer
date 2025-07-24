using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Controllers
{
    /// <summary>
    /// API controller for commit-related operations
    /// Mirrors the console menu options 1, 2, 3
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CommitsController : ControllerBase
    {
        private readonly IGitHubService _gitHubService;
        private readonly ICodeReviewService _codeReviewService;
        private readonly IRepositoryManagementService _repositoryService;

        public CommitsController(
            IGitHubService gitHubService,
            ICodeReviewService codeReviewService,
            IRepositoryManagementService repositoryService)
        {
            _gitHubService = gitHubService;
            _codeReviewService = codeReviewService;
            _repositoryService = repositoryService;
        }

        /// <summary>
        /// Lists recent commits (Menu option 1)
        /// </summary>
        /// <param name="count">Number of commits to retrieve (default: 10)</param>
        /// <returns>List of recent commits</returns>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentCommits([FromQuery] int count = 10)
        {
            try
            {
                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();
                var commits = await _gitHubService.GetRecentCommitsAsync(count);

                return Ok(new
                {
                    Repository = $"{owner}/{name}",
                    Count = commits.Count,
                    Commits = commits.Select(c => new
                    {
                        Sha = c.Sha,
                        Message = c.Commit.Message,
                        Author = c.Commit.Author.Name,
                        Date = c.Commit.Author.Date,
                        Url = c.HtmlUrl
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
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

                return Ok(new
                {
                    Repository = $"{owner}/{name}",
                    CommitSha = sha,
                    Commit = new
                    {
                        Sha = commit.Sha,
                        Message = commit.Commit.Message,
                        Author = commit.Commit.Author.Name,
                        Date = commit.Commit.Author.Date
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
    }
}
