namespace AICodeReviewer.Models
{
    /// <summary>
    /// Represents token usage information from Azure OpenAI API
    /// </summary>
    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
}
