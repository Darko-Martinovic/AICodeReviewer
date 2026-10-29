namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Code review process configuration
    /// </summary>
    public class CodeReviewSettings
    {
        public int MaxFilesToReview { get; set; } = 3;
        public int MaxIssuesInSummary { get; set; } = 3;
        /// <summary>
        /// Controls visibility of token usage and cost information in all review displays
        /// </summary>
        public bool ShowTokenMetrics { get; set; } = true;
        /// <summary>
        /// "diff" reviews what a commit or pull request changed, with the rest of the file as context;
        /// "file" reviews every file it touches as a whole
        /// </summary>
        public string ReviewMode { get; set; } = "diff";
    }
}
