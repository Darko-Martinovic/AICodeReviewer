using Octokit;

namespace AICodeReviewer.Services.Interfaces
{
    /// <summary>
    /// Interface for GitHub service operations
    /// </summary>
    public interface IGitHubService
    {
        Task InitializeAsync();
        Task<IReadOnlyList<string>> GetBranchesAsync();
        Task<IReadOnlyList<GitHubCommit>> GetCommitsAsync(string? branch = null);
        Task<IReadOnlyList<GitHubCommit>> GetRecentCommitsAsync(int count = 10, string? branch = null);
        Task<GitHubCommit> GetCommitDetailAsync(string sha);
        Task<GitHubCommit> GetCommitAsync(string sha);
        Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync();
        Task<IReadOnlyList<PullRequest>> GetOpenPullRequestsAsync();
        Task<PullRequest> GetPullRequestAsync(int number);
        Task<IReadOnlyList<PullRequestFile>> GetPullRequestFilesAsync(int pullRequestNumber);
        Task<string> GetFileContentAsync(string fileName);
        Task<string> GetFileContentFromBranchAsync(string fileName, string branch);
        Task<(IReadOnlyList<PullRequestFile> files, string headBranch)> GetPullRequestFilesWithBranchAsync(int prNumber);
        Task<(string Owner, string Name)> GetRepositoryInfoAsync();
        Task<Repository> GetRepositoryDetailsAsync();
        Task<bool> PostPullRequestCommentAsync(int prNumber, string comment);
        void UpdateRepository(string owner, string name);
    }
}
