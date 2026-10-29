using System.Text.Json.Serialization;

namespace AICodeReviewer.Models.Review
{
    /// <summary>
    /// One issue the reviewer reports, as the model returns it in structured JSON
    /// </summary>
    public class Finding
    {
        [JsonPropertyName("line")]
        public int Line { get; set; }

        [JsonPropertyName("end_line")]
        public int EndLine { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = "";

        [JsonPropertyName("suggestion")]
        public string Suggestion { get; set; } = "";
    }

    /// <summary>
    /// The model's whole answer: every finding in one file
    /// </summary>
    public class ReviewFindings
    {
        [JsonPropertyName("findings")]
        public List<Finding> Findings { get; set; } = new();
    }

    /// <summary>
    /// The review of one file: the findings, the model used and what it cost
    /// </summary>
    public class ReviewOutput
    {
        public string FileName { get; set; } = "";
        public string Model { get; set; } = "";
        public List<Finding> Findings { get; set; } = new();
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public decimal Cost { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
