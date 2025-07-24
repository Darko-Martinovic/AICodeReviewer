namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Teams simulation timing configuration
    /// </summary>
    public class SimulationDelaysSettings
    {
        public int WebhookPreparation { get; set; } = 500;
        public int WebhookConnection { get; set; } = 300;
        public int MessageSending { get; set; } = 400;
        public int DeliveryConfirmation { get; set; } = 200;
        public int UserInteraction { get; set; } = 800;
        public int BetweenResponses { get; set; } = 600;
        public int MentionNotification { get; set; } = 400;
        public int MetricsDisplay { get; set; } = 300;
    }
}
