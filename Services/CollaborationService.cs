using AICodeReviewer.Models.Collaboration;
using AICodeReviewer.Services.Interfaces;
using System.Collections.Concurrent;

namespace AICodeReviewer.Services
{
    public class CollaborationService : ICollaborationService
    {
        private readonly ConcurrentDictionary<string, ReviewSession> _sessions = new();
        private readonly ILogger<CollaborationService> _logger;
        private readonly string[] _userColors = new[]
        {
            "#3B82F6", "#EF4444", "#10B981", "#F59E0B", "#8B5CF6",
            "#EC4899", "#06B6D4", "#84CC16", "#F97316", "#6366F1"
        };

        public CollaborationService(ILogger<CollaborationService> logger)
        {
            _logger = logger;
        }

        public async Task<ReviewSession> CreateSessionAsync(string commitSha, string repositoryFullName, string creatorUserId, string creatorUserName)
        {
            var session = new ReviewSession
            {
                CommitSha = commitSha,
                RepositoryFullName = repositoryFullName
            };

            _sessions.TryAdd(session.Id, session);
            _logger.LogInformation("Created review session {SessionId} for commit {CommitSha}", session.Id, commitSha);

            return await Task.FromResult(session);
        }

        public async Task<ReviewSession?> GetSessionAsync(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return await Task.FromResult(session);
        }

        public async Task<ReviewSession?> GetSessionByCommitAsync(string commitSha, string repositoryFullName)
        {
            var session = _sessions.Values
                .FirstOrDefault(s => s.CommitSha == commitSha &&
                               s.RepositoryFullName == repositoryFullName &&
                               s.Status == "active");

            return await Task.FromResult(session);
        }

        public async Task<List<ReviewSession>> GetActiveSessionsAsync()
        {
            var activeSessions = _sessions.Values
                .Where(s => s.Status == "active")
                .OrderByDescending(s => s.LastActivity)
                .ToList();

            return await Task.FromResult(activeSessions);
        }

        public async Task<List<ReviewSession>> GetActiveSessionsForRepositoryAsync(string repositoryFullName)
        {
            var sessions = _sessions.Values
                .Where(s => s.RepositoryFullName == repositoryFullName && s.Status == "active")
                .OrderByDescending(s => s.LastActivity)
                .ToList();

            return await Task.FromResult(sessions);
        }

        public async Task<bool> JoinSessionAsync(string sessionId, SessionParticipant participant)
        {
            _logger.LogInformation("🔵 JoinSessionAsync called - SessionId: {SessionId}, UserId: {UserId}",
                sessionId, participant.UserId);

            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                _logger.LogWarning("🔵 Session {SessionId} not found, attempting to create it", sessionId);

                // Try to parse the sessionId to extract commit and repository info
                // SessionId format should be: repositoryFullName-commitSha
                var parts = sessionId.Split('-');
                if (parts.Length >= 2)
                {
                    var commitSha = parts[^1]; // Last part is commit SHA
                    var repositoryFullName = string.Join("-", parts[..^1]); // Everything before last part

                    _logger.LogInformation("🔵 Creating session for commit {CommitSha} in repository {Repository}",
                        commitSha, repositoryFullName);

                    session = new ReviewSession
                    {
                        Id = sessionId,
                        CommitSha = commitSha,
                        RepositoryFullName = repositoryFullName,
                        CreatedAt = DateTime.UtcNow,
                        Status = "active"
                    };

                    _sessions.TryAdd(sessionId, session);
                    _logger.LogInformation("🔵 Created new session {SessionId}", sessionId);
                }
                else
                {
                    _logger.LogError("🔵 Invalid sessionId format: {SessionId}", sessionId);
                    return false;
                }
            }

            // Assign a color if not already assigned
            if (string.IsNullOrEmpty(participant.UserColor))
            {
                var usedColors = session.Participants.Select(p => p.UserColor).Where(c => !string.IsNullOrEmpty(c)).ToHashSet();
                participant.UserColor = _userColors.FirstOrDefault(c => !usedColors.Contains(c)) ?? _userColors[0];
            }

            // Remove any existing participant with same userId
            session.Participants.RemoveAll(p => p.UserId == participant.UserId);

            // Add new participant
            session.Participants.Add(participant);
            session.LastActivity = DateTime.UtcNow;

            _logger.LogInformation("🔵 User {UserId} joined session {SessionId}. Total participants: {Count}",
                participant.UserId, sessionId, session.Participants.Count);
            return await Task.FromResult(true);
        }

        public async Task<bool> LeaveSessionAsync(string sessionId, string connectionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            var participant = session.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (participant != null)
            {
                session.Participants.Remove(participant);
                session.LastActivity = DateTime.UtcNow;
                _logger.LogInformation("User {UserId} left session {SessionId}", participant.UserId, sessionId);
            }

            // Archive session if no participants left
            if (!session.Participants.Any())
            {
                session.Status = "archived";
                _logger.LogInformation("Session {SessionId} archived - no participants", sessionId);
            }

            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateCursorAsync(string sessionId, string userId, CursorPosition position)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);
            if (participant != null)
            {
                participant.CurrentCursor = position;
                session.LastActivity = DateTime.UtcNow;
            }

            return await Task.FromResult(true);
        }

        public async Task<LiveComment> AddCommentAsync(string sessionId, LiveComment comment)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                throw new InvalidOperationException($"Session {sessionId} not found");

            session.Comments.Add(comment);
            session.LastActivity = DateTime.UtcNow;

            _logger.LogInformation("Comment {CommentId} added to session {SessionId}", comment.Id, sessionId);
            return await Task.FromResult(comment);
        }

        public async Task<bool> UpdateCommentAsync(string sessionId, LiveComment comment)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            var existingComment = session.Comments.FirstOrDefault(c => c.Id == comment.Id);
            if (existingComment != null)
            {
                existingComment.Content = comment.Content;
                existingComment.UpdatedAt = DateTime.UtcNow;
                session.LastActivity = DateTime.UtcNow;
                return true;
            }

            return await Task.FromResult(false);
        }

        public async Task<bool> DeleteCommentAsync(string sessionId, string commentId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            var comment = session.Comments.FirstOrDefault(c => c.Id == commentId);
            if (comment != null)
            {
                session.Comments.Remove(comment);
                session.LastActivity = DateTime.UtcNow;
                return true;
            }

            return await Task.FromResult(false);
        }

        public async Task<bool> ResolveCommentAsync(string sessionId, string commentId, bool isResolved)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            var comment = session.Comments.FirstOrDefault(c => c.Id == commentId);
            if (comment != null)
            {
                comment.IsResolved = isResolved;
                session.LastActivity = DateTime.UtcNow;
                return true;
            }

            return await Task.FromResult(false);
        }

        public async Task<CommentReply> AddCommentReplyAsync(string sessionId, string commentId, CommentReply reply)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                throw new InvalidOperationException($"Session {sessionId} not found");

            var comment = session.Comments.FirstOrDefault(c => c.Id == commentId);
            if (comment == null)
                throw new InvalidOperationException($"Comment {commentId} not found");

            comment.Replies.Add(reply);
            session.LastActivity = DateTime.UtcNow;

            return await Task.FromResult(reply);
        }

        public async Task<bool> ArchiveSessionAsync(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            session.Status = "archived";
            session.LastActivity = DateTime.UtcNow;
            _logger.LogInformation("Session {SessionId} archived", sessionId);

            return await Task.FromResult(true);
        }

        public async Task CleanupInactiveSessionsAsync()
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24); // Archive sessions older than 24 hours
            var inactiveSessions = _sessions.Values
                .Where(s => s.LastActivity < cutoffTime && s.Status == "active")
                .ToList();

            foreach (var session in inactiveSessions)
            {
                session.Status = "archived";
                _logger.LogInformation("Auto-archived inactive session {SessionId}", session.Id);
            }

            await Task.CompletedTask;
        }

        public async Task<bool> UpdateSessionActivityAsync(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            session.LastActivity = DateTime.UtcNow;
            return await Task.FromResult(true);
        }

        public async Task<List<CursorPosition>> GetActiveCursorsAsync(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return new List<CursorPosition>();

            var cursors = session.Participants
                .Where(p => p.CurrentCursor != null && p.IsActive)
                .Select(p => p.CurrentCursor!)
                .ToList();

            return await Task.FromResult(cursors);
        }
    }
}
