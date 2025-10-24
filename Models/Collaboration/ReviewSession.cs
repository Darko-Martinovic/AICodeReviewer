using System.ComponentModel.DataAnnotations;

namespace AICodeReviewer.Models.Collaboration
{
    public class ReviewSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CommitSha { get; set; } = string.Empty;
        public string RepositoryFullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<SessionParticipant> Participants { get; set; } = new();
        public List<LiveComment> Comments { get; set; } = new();
        public string Status { get; set; } = "active"; // active, completed, archived
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        // Track the currently active file in the session
        public string? CurrentFileName { get; set; }
        public string? CurrentFileContent { get; set; }
        public string? CurrentFileLanguage { get; set; }
    }

    public class SessionParticipant
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public CursorPosition? CurrentCursor { get; set; }
        public bool IsActive { get; set; } = true;
        public string UserColor { get; set; } = string.Empty;
    }

    public class CursorPosition
    {
        public string FileName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int Column { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class LiveComment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsResolved { get; set; } = false;
        public string CommentType { get; set; } = "general"; // general, suggestion, question, issue
        public List<CommentReply> Replies { get; set; } = new();
    }

    public class CommentReply
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
