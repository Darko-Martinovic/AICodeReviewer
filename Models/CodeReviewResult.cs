namespace AICodeReviewer.Models
{
    /// <summary>
    /// Represents the result of an AI code review operation
    /// </summary>
    public class CodeReviewResult
    {
        public List<string> ReviewedFiles { get; set; } = new();
        public List<string> AllIssues { get; set; } = new();
        public List<DetailedIssue> DetailedIssues { get; set; } = new();
        public ReviewMetrics Metrics { get; set; } = new();
        public int IssueCount => AllIssues.Count;
        public bool HasIssues => AllIssues.Any();
    }

    /// <summary>
    /// Represents a detailed code review issue with context
    /// </summary>
    public class DetailedIssue
    {
        public string FileName { get; set; } = "";
        public string Category { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Recommendation { get; set; } = "";
        public int? LineNumber { get; set; }
        public string CodeSnippet { get; set; } = "";
    }
}
