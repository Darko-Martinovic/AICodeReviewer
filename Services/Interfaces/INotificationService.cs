namespace AICodeReviewer.Services.Interfaces
{
    /// <summary>
    /// Interface for notification service operations
    /// </summary>
    public interface INotificationService
    {
        Task SendTeamsNotificationAsync(string commitSha, string author, List<string> reviewedFiles, int issueCount, List<string> topIssues);
    }
}
