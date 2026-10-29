namespace AICodeReviewer.Models.Review
{
    /// <summary>
    /// A model the reviewer can use, as listed in Configuration/models.json
    /// </summary>
    public class ModelInfo
    {
        public string Key { get; set; } = "";
        public string Provider { get; set; } = "";
        public string Id { get; set; } = "";
        public decimal InputPerMillion { get; set; }
        public decimal OutputPerMillion { get; set; }

        /// <summary>
        /// Anthropic only: low, medium, high, xhigh or max; the model's default when empty
        /// </summary>
        public string? Effort { get; set; }

        /// <summary>
        /// Chat completions providers only (Azure OpenAI and OpenAI-compatible APIs)
        /// </summary>
        public float? Temperature { get; set; }

        /// <summary>
        /// Azure OpenAI only: the API version, which must support structured outputs
        /// </summary>
        public string? ApiVersion { get; set; }

        /// <summary>
        /// OpenAI-compatible APIs only: the address that ends before /chat/completions
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// OpenAI-compatible APIs only: the setting that holds the API key, if one is needed
        /// </summary>
        public string? ApiKeySetting { get; set; }

        public string? Notes { get; set; }

        public decimal CostOf(int inputTokens, int outputTokens) =>
            (inputTokens * InputPerMillion + outputTokens * OutputPerMillion) / 1_000_000m;
    }
}
