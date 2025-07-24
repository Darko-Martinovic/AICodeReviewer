namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Teams response count configuration
    /// </summary>
    public class ResponseRangeSettings
    {
        public int MinResponses { get; set; } = 1;
        public int MaxResponses { get; set; } = 3;
    }
}
