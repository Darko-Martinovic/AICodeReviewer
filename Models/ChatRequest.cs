namespace AICodeReviewer.Models
{
    /// <summary>
    /// Represents a chat request to Azure OpenAI API
    /// </summary>
    public class ChatRequest
    {
        public ChatMessage[] messages { get; set; } = Array.Empty<ChatMessage>();
        public int max_tokens { get; set; } = 1000;
        public float temperature { get; set; } = 0.3f;
        public float top_p { get; set; } = 0.95f;
    }
}
