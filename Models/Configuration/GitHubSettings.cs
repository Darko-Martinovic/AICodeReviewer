namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// GitHub service configuration
    /// </summary>
    public class GitHubSettings
    {
        public int MaxCommitsToList { get; set; } = 10;
        public int MaxPullRequestsToList { get; set; } = 5;
        public RepositoryInfo DefaultRepository { get; set; } = new();
        public List<RepositoryInfo> RepositoryHistory { get; set; } = new();
        public RepositoryFilterSettings RepositoryFilter { get; set; } = new();
    }
}
