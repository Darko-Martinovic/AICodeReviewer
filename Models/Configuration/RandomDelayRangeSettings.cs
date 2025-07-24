namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Random delay range for Teams simulation
    /// </summary>
    public class RandomDelayRangeSettings
    {
        public int MinMs { get; set; } = 500;
        public int MaxMs { get; set; } = 1200;
    }
}
