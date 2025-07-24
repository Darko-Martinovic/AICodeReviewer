namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Teams notification simulation configuration
    /// </summary>
    public class TeamsSettings
    {
        public SimulationDelaysSettings SimulationDelays { get; set; } = new();
        public List<string> TeamMembers { get; set; } = new();
        public ResponseRangeSettings ResponseRange { get; set; } = new();
        public RandomDelayRangeSettings RandomDelayRange { get; set; } = new();
    }
}
