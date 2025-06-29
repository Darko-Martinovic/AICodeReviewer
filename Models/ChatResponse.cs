namespace AICodeReviewer.Models
{
    /// <summary>
    /// Represents the response from Azure OpenAI API
    /// </summary>
    public class ChatResponse
    {
        public ChatChoice[] choices { get; set; } = Array.Empty<ChatChoice>();
        public Usage usage { get; set; } = new();
    }
}
