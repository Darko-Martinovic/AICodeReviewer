using Microsoft.AspNetCore.SignalR;
using AICodeReviewer.Models.Collaboration;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Hubs
{
    public class CollaborationHub : Hub
    {
        private readonly ICollaborationService _collaborationService;
        private readonly ILogger<CollaborationHub> _logger;

        public CollaborationHub(ICollaborationService collaborationService, ILogger<CollaborationHub> logger)
        {
            _collaborationService = collaborationService;
            _logger = logger;
        }

        public async Task JoinSession(string sessionId, string userId, string userName, string avatarUrl = "")
        {
            _logger.LogInformation("🔵 JoinSession called - SessionId: {SessionId}, UserId: {UserId}, UserName: {UserName}",
                sessionId, userId, userName);

            try
            {
                var participant = new SessionParticipant
                {
                    ConnectionId = Context.ConnectionId,
                    UserId = userId,
                    UserName = userName,
                    AvatarUrl = avatarUrl
                };

                _logger.LogInformation("🔵 Calling CollaborationService.JoinSessionAsync");
                var success = await _collaborationService.JoinSessionAsync(sessionId, participant);
                _logger.LogInformation("🔵 JoinSessionAsync result: {Success}", success);

                if (success)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

                    var userPresenceMessage = new UserPresenceMessage
                    {
                        UserId = userId,
                        UserName = userName,
                        AvatarUrl = avatarUrl,
                        UserColor = participant.UserColor,
                        Action = "joined"
                    };

                    // Notify others in the session
                    await Clients.OthersInGroup(sessionId).SendAsync("UserJoined", userPresenceMessage);

                    // Send current session state to the new participant
                    var session = await _collaborationService.GetSessionAsync(sessionId);
                    if (session != null)
                    {
                        _logger.LogInformation("🔵 Sending SessionState - Participants count: {Count}",
                            session.Participants.Count);

                        await Clients.Caller.SendAsync("SessionState", new
                        {
                            participants = session.Participants, // Include all participants, including current user
                            comments = session.Comments,
                            cursors = session.Participants.Where(p => p.CurrentCursor != null).Select(p => new CursorUpdateMessage
                            {
                                UserId = p.UserId,
                                UserName = p.UserName,
                                UserColor = p.UserColor,
                                Position = p.CurrentCursor!
                            })
                        });
                    }

                    _logger.LogInformation("User {UserId} joined session {SessionId}", userId, sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining session {SessionId} for user {UserId}", sessionId, userId);
                await Clients.Caller.SendAsync("Error", "Failed to join session");
            }
        }

        public async Task LeaveSession(string sessionId)
        {
            try
            {
                await _collaborationService.LeaveSessionAsync(sessionId, Context.ConnectionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);

                // Notify others that user left
                await Clients.OthersInGroup(sessionId).SendAsync("UserLeft", Context.ConnectionId);

                _logger.LogInformation("Connection {ConnectionId} left session {SessionId}", Context.ConnectionId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving session {SessionId}", sessionId);
            }
        }

        public async Task UpdateCursor(string sessionId, string userId, CursorPosition position)
        {
            try
            {
                await _collaborationService.UpdateCursorAsync(sessionId, userId, position);

                var session = await _collaborationService.GetSessionAsync(sessionId);
                if (session != null)
                {
                    var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);
                    if (participant != null)
                    {
                        var cursorMessage = new CursorUpdateMessage
                        {
                            UserId = userId,
                            UserName = participant.UserName,
                            UserColor = participant.UserColor,
                            Position = position
                        };

                        await Clients.OthersInGroup(sessionId).SendAsync("CursorMoved", cursorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cursor for user {UserId} in session {SessionId}", userId, sessionId);
            }
        }

        public async Task SendComment(string sessionId, LiveComment comment)
        {
            try
            {
                var savedComment = await _collaborationService.AddCommentAsync(sessionId, comment);

                var commentMessage = new CommentMessage
                {
                    Comment = savedComment,
                    Action = "create"
                };

                await Clients.Group(sessionId).SendAsync("CommentAdded", commentMessage);

                _logger.LogInformation("Comment added to session {SessionId} by user {UserId}", sessionId, comment.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to session {SessionId}", sessionId);
                await Clients.Caller.SendAsync("Error", "Failed to add comment");
            }
        }

        public async Task UpdateComment(string sessionId, LiveComment comment)
        {
            try
            {
                var success = await _collaborationService.UpdateCommentAsync(sessionId, comment);
                if (success)
                {
                    var commentMessage = new CommentMessage
                    {
                        Comment = comment,
                        Action = "update"
                    };

                    await Clients.Group(sessionId).SendAsync("CommentUpdated", commentMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {CommentId} in session {SessionId}", comment.Id, sessionId);
            }
        }

        public async Task DeleteComment(string sessionId, string commentId)
        {
            try
            {
                var success = await _collaborationService.DeleteCommentAsync(sessionId, commentId);
                if (success)
                {
                    await Clients.Group(sessionId).SendAsync("CommentDeleted", commentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId} in session {SessionId}", commentId, sessionId);
            }
        }

        public async Task ResolveComment(string sessionId, string commentId, bool isResolved)
        {
            try
            {
                var success = await _collaborationService.ResolveCommentAsync(sessionId, commentId, isResolved);
                if (success)
                {
                    await Clients.Group(sessionId).SendAsync("CommentResolved", new { commentId, isResolved });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving comment {CommentId} in session {SessionId}", commentId, sessionId);
            }
        }

        public async Task AddCommentReply(string sessionId, string commentId, CommentReply reply)
        {
            try
            {
                var savedReply = await _collaborationService.AddCommentReplyAsync(sessionId, commentId, reply);

                await Clients.Group(sessionId).SendAsync("CommentReplyAdded", new { commentId, reply = savedReply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reply to comment {CommentId} in session {SessionId}", commentId, sessionId);
            }
        }

        public async Task NotifyTyping(string sessionId, string userId, string fileName, bool isTyping)
        {
            try
            {
                await Clients.OthersInGroup(sessionId).SendAsync("UserTyping", new { userId, fileName, isTyping });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending typing notification for user {UserId} in session {SessionId}", userId, sessionId);
            }
        }

        public async Task ChangeFile(string sessionId, string userId, string fileName)
        {
            try
            {
                var fileViewMessage = new FileViewMessage
                {
                    UserId = userId,
                    UserName = Context.User?.Identity?.Name ?? "Unknown",
                    FileName = fileName
                };

                await Clients.OthersInGroup(sessionId).SendAsync("UserChangedFile", fileViewMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying file change for user {UserId} in session {SessionId}", userId, sessionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                // Find all sessions this connection was part of and leave them
                var allSessions = await _collaborationService.GetActiveSessionsAsync();
                foreach (var session in allSessions)
                {
                    var participant = session.Participants.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                    if (participant != null)
                    {
                        await _collaborationService.LeaveSessionAsync(session.Id, Context.ConnectionId);
                        await Clients.OthersInGroup(session.Id).SendAsync("UserLeft", Context.ConnectionId);
                    }
                }

                _logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling disconnection for connection {ConnectionId}", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
