using AICodeReviewer.Models;

namespace AICodeReviewer.Services.Interfaces
{
    /// <summary>
    /// Interface for caching code review results
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get cached review result for a commit
        /// </summary>
        /// <param name="commitSha">Commit SHA</param>
        /// <returns>Cached result or null if not found</returns>
        Task<CodeReviewResult?> GetCommitReviewAsync(string commitSha);

        /// <summary>
        /// Cache a commit review result
        /// </summary>
        /// <param name="commitSha">Commit SHA</param>
        /// <param name="result">Review result to cache</param>
        Task SetCommitReviewAsync(string commitSha, CodeReviewResult result);

        /// <summary>
        /// Get cached review result for a pull request
        /// </summary>
        /// <param name="pullRequestNumber">PR number</param>
        /// <returns>Cached result or null if not found</returns>
        Task<CodeReviewResult?> GetPullRequestReviewAsync(int pullRequestNumber);

        /// <summary>
        /// Cache a pull request review result
        /// </summary>
        /// <param name="pullRequestNumber">PR number</param>
        /// <param name="result">Review result to cache</param>
        Task SetPullRequestReviewAsync(int pullRequestNumber, CodeReviewResult result);

        /// <summary>
        /// Check if a commit review is cached
        /// </summary>
        /// <param name="commitSha">Commit SHA</param>
        /// <returns>True if cached</returns>
        Task<bool> HasCommitReviewAsync(string commitSha);

        /// <summary>
        /// Check if a pull request review is cached
        /// </summary>
        /// <param name="pullRequestNumber">PR number</param>
        /// <returns>True if cached</returns>
        Task<bool> HasPullRequestReviewAsync(int pullRequestNumber);

        /// <summary>
        /// Clear all cached reviews
        /// </summary>
        Task ClearAllAsync();

        /// <summary>
        /// Clear cached review for a specific commit
        /// </summary>
        /// <param name="commitSha">Commit SHA</param>
        Task ClearCommitReviewAsync(string commitSha);

        /// <summary>
        /// Clear cached review for a specific pull request
        /// </summary>
        /// <param name="pullRequestNumber">PR number</param>
        Task ClearPullRequestReviewAsync(int pullRequestNumber);
    }
}
