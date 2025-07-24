using AICodeReviewer.Services;

namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Application configuration settings loaded from appsettings.json
    /// </summary>
    public class AppSettings
    {
        public bool DebugLogging { get; set; } = false;
        public AzureOpenAISettings AzureOpenAI { get; set; } = new();
        public CodeReviewSettings CodeReview { get; set; } = new();
        public TeamsSettings Teams { get; set; } = new();
        public GitHubSettings GitHub { get; set; } = new();
        public JiraSettings Jira { get; set; } = new();
    }
}
