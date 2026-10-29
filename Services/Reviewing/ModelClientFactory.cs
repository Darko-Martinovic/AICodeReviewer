using Anthropic;
using AICodeReviewer.Models.Review;

namespace AICodeReviewer.Services.Reviewing
{
    /// <summary>
    /// Builds a provider's client from the keys in configuration: Anthropic:ApiKey (or ANTHROPIC_API_KEY),
    /// AzureOpenAI:Endpoint, AzureOpenAI:ApiKey and AzureOpenAI:DeploymentName, or a model's ApiKeySetting
    /// </summary>
    public class ModelClientFactory(IConfiguration configuration, HttpClient httpClient) : IModelClientFactory
    {
        private readonly Lazy<AnthropicClient> _anthropic = new(() =>
        {
            var apiKey = configuration["Anthropic:ApiKey"];
            // Without a key in configuration the SDK reads ANTHROPIC_API_KEY
            return string.IsNullOrEmpty(apiKey) ? new AnthropicClient() : new AnthropicClient { ApiKey = apiKey };
        });

        public IModelClient Create(ModelInfo model) => model.Provider switch
        {
            "anthropic" => new AnthropicModelClient(_anthropic.Value, model),
            "azure-openai" => Azure(model),
            "openai-compatible" => new ChatCompletionsModelClient(
                httpClient,
                model,
                $"{Required(model.BaseUrl, $"baseUrl of model '{model.Key}'").TrimEnd('/')}/chat/completions",
                model.ApiKeySetting is null ? null : Required(configuration[model.ApiKeySetting], model.ApiKeySetting),
                azure: false),
            _ => throw new InvalidOperationException($"Unknown provider '{model.Provider}' for model '{model.Key}'")
        };

        private ChatCompletionsModelClient Azure(ModelInfo model)
        {
            var endpoint = Required(configuration["AzureOpenAI:Endpoint"], "AzureOpenAI:Endpoint");
            var deployment = string.IsNullOrEmpty(model.Id)
                ? Required(configuration["AzureOpenAI:DeploymentName"], "AzureOpenAI:DeploymentName")
                : model.Id;
            var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/chat/completions" +
                $"?api-version={Required(model.ApiVersion, $"apiVersion of model '{model.Key}'")}";
            return new ChatCompletionsModelClient(
                httpClient, model, url, Required(configuration["AzureOpenAI:ApiKey"], "AzureOpenAI:ApiKey"), azure: true);
        }

        private static string Required(string? value, string name) =>
            string.IsNullOrEmpty(value) ? throw new InvalidOperationException($"{name} is not set") : value;
    }
}
