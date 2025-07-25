using AICodeReviewer.Models;

namespace AICodeReviewer.Services.Interfaces
{
    /// <summary>
    /// Interface for Jira service operations
    /// </summary>
    public interface IJiraService : IDisposable
    {
        Task<string> CreateIssueAsync(
            string project,
            string issueType,
            string summary,
            string description,
            string priority = "Medium",
            string assignee = ""
        );
        List<string> ExtractTicketKeysFromTitle(string prTitle);
        Task UpdateTicketsWithReviewResultsAsync(
            List<string> ticketKeys,
            string prNumber,
            string author,
            int issueCount,
            List<string> reviewedFiles,
            List<string> topIssues,
            List<DetailedIssue>? detailedIssues = null
        );
        object CreateJiraCommentAdf(
            string prNumber,
            string author,
            int issueCount,
            List<string> reviewedFiles,
            List<string> topIssues,
            List<DetailedIssue>? detailedIssues = null
        );
        string CreateJiraComment(
            string prNumber,
            string author,
            int issueCount,
            List<string> reviewedFiles,
            List<string> topIssues
        );
    }
}
