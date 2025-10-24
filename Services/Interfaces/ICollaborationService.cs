using AICodeReviewer.Models.Collaboration;

namespace AICodeReviewer.Services.Interfaces
{
    public interface ICollaborationService
    {
        Task<ReviewSession> CreateSessionAsync(string commitSha, string repositoryFullName, string creatorUserId, string creatorUserName);
        Task<ReviewSession?> GetSessionAsync(string sessionId);
        Task<ReviewSession?> GetSessionByCommitAsync(string commitSha, string repositoryFullName);
        Task<List<ReviewSession>> GetActiveSessionsAsync();
        Task<List<ReviewSession>> GetActiveSessionsForRepositoryAsync(string repositoryFullName);
        Task<bool> JoinSessionAsync(string sessionId, SessionParticipant participant);
        Task<bool> LeaveSessionAsync(string sessionId, string connectionId);
        Task<bool> UpdateCursorAsync(string sessionId, string userId, CursorPosition position);
        Task<LiveComment> AddCommentAsync(string sessionId, LiveComment comment);
        Task<bool> UpdateCommentAsync(string sessionId, LiveComment comment);
        Task<bool> DeleteCommentAsync(string sessionId, string commentId);
        Task<bool> ResolveCommentAsync(string sessionId, string commentId, bool isResolved);
        Task<CommentReply> AddCommentReplyAsync(string sessionId, string commentId, CommentReply reply);
        Task<bool> ArchiveSessionAsync(string sessionId);
        Task CleanupInactiveSessionsAsync();
        Task<bool> UpdateSessionActivityAsync(string sessionId);
        Task<List<CursorPosition>> GetActiveCursorsAsync(string sessionId);
        Task<bool> SetCurrentFileAsync(string sessionId, string fileName, string fileContent, string fileLanguage);
    }
}
