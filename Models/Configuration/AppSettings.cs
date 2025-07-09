namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Application configuration settings loaded from appsettings.json
    /// </summary>
    public class AppSettings
    {
        public AzureOpenAISettings AzureOpenAI { get; set; } = new();
        public CodeReviewSettings CodeReview { get; set; } = new();
        public TeamsSettings Teams { get; set; } = new();
        public GitHubSettings GitHub { get; set; } = new();
        public JiraSettings Jira { get; set; } = new();
    }

    /// <summary>
    /// Azure OpenAI service configuration
    /// </summary>
    public class AzureOpenAISettings
    {
        public string ApiVersion { get; set; } = "2024-02-01";
        public float Temperature { get; set; } = 0.3f;
        public int MaxTokens { get; set; } = 2500;
        public int ContentLimit { get; set; } = 15000;
        public string SystemPrompt { get; set; } = string.Empty;
        public LanguagePrompts LanguagePrompts { get; set; } = new();
    }

    /// <summary>
    /// Code review process configuration
    /// </summary>
    public class CodeReviewSettings
    {
        public int MaxFilesToReview { get; set; } = 3;
        public int MaxIssuesInSummary { get; set; } = 3;
    }

    /// <summary>
    /// Teams notification simulation configuration
    /// </summary>
    public class TeamsSettings
    {
        public SimulationDelaysSettings SimulationDelays { get; set; } = new();
        public List<string> TeamMembers { get; set; } = new();
        public ResponseRangeSettings ResponseRange { get; set; } = new();
        public RandomDelayRangeSettings RandomDelayRange { get; set; } = new();
    }

    /// <summary>
    /// Teams simulation timing configuration
    /// </summary>
    public class SimulationDelaysSettings
    {
        public int WebhookPreparation { get; set; } = 500;
        public int WebhookConnection { get; set; } = 300;
        public int MessageSending { get; set; } = 400;
        public int DeliveryConfirmation { get; set; } = 200;
        public int UserInteraction { get; set; } = 800;
        public int BetweenResponses { get; set; } = 600;
        public int MentionNotification { get; set; } = 400;
        public int MetricsDisplay { get; set; } = 300;
    }

    /// <summary>
    /// Teams response count configuration
    /// </summary>
    public class ResponseRangeSettings
    {
        public int MinResponses { get; set; } = 1;
        public int MaxResponses { get; set; } = 3;
    }

    /// <summary>
    /// Random delay range for Teams simulation
    /// </summary>
    public class RandomDelayRangeSettings
    {
        public int MinMs { get; set; } = 500;
        public int MaxMs { get; set; } = 1200;
    }

    /// <summary>
    /// GitHub service configuration
    /// </summary>
    public class GitHubSettings
    {
        public int MaxCommitsToList { get; set; } = 10;
        public int MaxPullRequestsToList { get; set; } = 5;
    }

    /// <summary>
    /// Jira service configuration
    /// </summary>
    public class JiraSettings
    {
        public List<string> DefaultLabels { get; set; } = new();
        public Dictionary<string, string> SeverityLabels { get; set; } = new();
    }
}
