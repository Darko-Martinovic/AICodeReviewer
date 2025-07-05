using AICodeReviewer.Models;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Interface for Jira service operations
    /// </summary>
    public interface IJiraService : IDisposable
    {
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
