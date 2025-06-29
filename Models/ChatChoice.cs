namespace AICodeReviewer.Models
{
    /// <summary>
    /// Represents a choice in Azure OpenAI API response
    /// </summary>
    public class ChatChoice
    {
        public ChatMessage message { get; set; } = new();
        public string finish_reason { get; set; } = "";
    }
}
