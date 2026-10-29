namespace AICodeReviewer.Models
{
    /// <summary>
    /// Represents the tokens and cost of a model call
    /// </summary>
    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
        public decimal cost { get; set; }
    }
}
