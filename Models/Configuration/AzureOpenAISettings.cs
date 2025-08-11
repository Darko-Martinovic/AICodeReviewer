namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Azure OpenAI service configuration
    /// </summary>
    public class AzureOpenAISettings
    {
        public string ApiVersion { get; set; } = "2024-02-01";
        public float Temperature { get; set; } = 0.0f;
        public int MaxTokens { get; set; } = 2500;
        public int ContentLimit { get; set; } = 15000;
        public string SystemPrompt { get; set; } = string.Empty;
        public LanguagePrompts LanguagePrompts { get; set; } = new();
    }
}
