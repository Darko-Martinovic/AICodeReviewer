using AICodeReviewer.Models;
using Octokit;

namespace AICodeReviewer.Services.Interfaces
{
    /// <summary>
    /// Interface for code review service operations
    /// </summary>
    public interface ICodeReviewService
    {
        Task<CodeReviewResult> ReviewCommitAsync(IReadOnlyList<GitHubCommitFile> files);
        Task<CodeReviewResult> ReviewPullRequestAsync(IReadOnlyList<PullRequestFile> files);
    }
}
