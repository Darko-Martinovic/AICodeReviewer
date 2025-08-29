using AICodeReviewer.Models.Collaboration;

namespace AICodeReviewer.Models.Collaboration
{
    // WebSocket message types
    public class CursorUpdateMessage
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserColor { get; set; } = string.Empty;
        public CursorPosition Position { get; set; } = new();
    }

    public class CommentMessage
    {
        public LiveComment Comment { get; set; } = new();
        public string Action { get; set; } = string.Empty; // create, update, delete, resolve
    }

    public class UserPresenceMessage
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string UserColor { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // joined, left, typing
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class SessionStatusMessage
    {
        public string SessionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ParticipantCount { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class FileViewMessage
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
