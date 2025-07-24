using Octokit;

namespace AICodeReviewer.Services.Interfaces
{
    /// <summary>
    /// Interface for GitHub service operations
    /// </summary>
    public interface IGitHubService
    {
        Task InitializeAsync();
        Task<IReadOnlyList<GitHubCommit>> GetCommitsAsync();
        Task<IReadOnlyList<GitHubCommit>> GetRecentCommitsAsync(int count = 10);
        Task<GitHubCommit> GetCommitDetailAsync(string sha);
        Task<GitHubCommit> GetCommitAsync(string sha);
        Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync();
        Task<IReadOnlyList<PullRequest>> GetOpenPullRequestsAsync();
        Task<PullRequest> GetPullRequestAsync(int number);
        Task<IReadOnlyList<PullRequestFile>> GetPullRequestFilesAsync(int pullRequestNumber);
        Task<string> GetFileContentAsync(string fileName);
        Task<(string Owner, string Name)> GetRepositoryInfoAsync();
        Task<Repository> GetRepositoryDetailsAsync();
        Task<bool> PostPullRequestCommentAsync(int prNumber, string comment);
        void UpdateRepository(string owner, string name);
    }
}
