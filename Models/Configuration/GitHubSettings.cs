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
        public GitHubAppSettings GitHubApp { get; set; } = new();
    }

    /// <summary>
    /// GitHub App authentication settings
    /// </summary>
    public class GitHubAppSettings
    {
        public long AppId { get; set; } = 0;
        public string ClientId { get; set; } = string.Empty;
        public long InstallationId { get; set; } = 0;
        public string PrivateKeyPath { get; set; } = string.Empty;
        public bool UseAppAuthentication { get; set; } = false;
    }
}
