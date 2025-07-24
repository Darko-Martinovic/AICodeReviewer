namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Jira service configuration
    /// </summary>
    public class JiraSettings
    {
        public List<string> DefaultLabels { get; set; } = new();
        public Dictionary<string, string> SeverityLabels { get; set; } = new();
    }
}
