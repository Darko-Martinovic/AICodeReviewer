namespace AICodeReviewer.Models
{
    /// <summary>
    /// Represents the result of an AI code review operation
    /// </summary>
    public class CodeReviewResult
    {
        public List<string> ReviewedFiles { get; set; } = new();
        public List<string> AllIssues { get; set; } = new();
        public int IssueCount => AllIssues.Count;
        public bool HasIssues => AllIssues.Any();
    }
}
