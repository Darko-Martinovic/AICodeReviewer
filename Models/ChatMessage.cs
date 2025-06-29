namespace AICodeReviewer.Models
{
    /// <summary>
    /// Represents a chat message for Azure OpenAI API
    /// </summary>
    public class ChatMessage
    {
        public string role { get; set; } = "";
        public string content { get; set; } = "";
    }
}
