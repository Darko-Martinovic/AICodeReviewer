namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Code review process configuration
    /// </summary>
    public class CodeReviewSettings
    {
        public int MaxFilesToReview { get; set; } = 3;
        public int MaxIssuesInSummary { get; set; } = 3;
    }
}
