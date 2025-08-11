namespace AICodeReviewer.Models
{
    /// <summary>
    /// Represents a chat request to Azure OpenAI API
    /// </summary>
    public class ChatRequest
    {
        public ChatMessage[] messages { get; set; } = Array.Empty<ChatMessage>();
        public int max_tokens { get; set; } = 1000;
        public float temperature { get; set; } = 0.0f; // Default to deterministic for consistency
        public float top_p { get; set; } = 0.95f;
        public int? seed { get; set; } = null; // For deterministic results
    }
}
