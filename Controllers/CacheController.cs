using AICodeReviewer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Asp.Versioning;

namespace AICodeReviewer.Controllers
{
    /// <summary>
    /// Controller for managing code review cache
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [EnableCors("AllowReactApp")]
    public class CacheController : ControllerBase
    {
        private readonly ICacheService _cacheService;

        public CacheController(ICacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        /// <summary>
        /// Check if a commit review is cached
        /// </summary>
        /// <param name="commitSha">Commit SHA</param>
        /// <returns>True if cached</returns>
        [HttpGet("commit/{commitSha}/exists")]
        public async Task<ActionResult<bool>> HasCommitReview(string commitSha)
        {
            try
            {
                var exists = await _cacheService.HasCommitReviewAsync(commitSha);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error checking cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a pull request review is cached
        /// </summary>
        /// <param name="pullRequestNumber">PR number</param>
        /// <returns>True if cached</returns>
        [HttpGet("pullrequest/{pullRequestNumber}/exists")]
        public async Task<ActionResult<bool>> HasPullRequestReview(int pullRequestNumber)
        {
            try
            {
                var exists = await _cacheService.HasPullRequestReviewAsync(pullRequestNumber);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error checking cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all cached reviews
        /// </summary>
        /// <returns>Success message</returns>
        [HttpDelete("clear")]
        public async Task<ActionResult<string>> ClearAllCache()
        {
            try
            {
                await _cacheService.ClearAllAsync();
                return Ok("All cached reviews cleared successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error clearing cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear cached review for a specific commit
        /// </summary>
        /// <param name="commitSha">Commit SHA</param>
        /// <returns>Success message</returns>
        [HttpDelete("commit/{commitSha}")]
        public async Task<ActionResult<string>> ClearCommitReview(string commitSha)
        {
            try
            {
                await _cacheService.ClearCommitReviewAsync(commitSha);
                return Ok($"Cached review for commit {commitSha} cleared successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error clearing cached review: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear cached review for a specific pull request
        /// </summary>
        /// <param name="pullRequestNumber">PR number</param>
        /// <returns>Success message</returns>
        [HttpDelete("pullrequest/{pullRequestNumber}")]
        public async Task<ActionResult<string>> ClearPullRequestReview(int pullRequestNumber)
        {
            try
            {
                await _cacheService.ClearPullRequestReviewAsync(pullRequestNumber);
                return Ok($"Cached review for PR #{pullRequestNumber} cleared successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error clearing cached review: {ex.Message}");
            }
        }
    }
}
