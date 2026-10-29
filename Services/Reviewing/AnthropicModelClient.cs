using Anthropic;
using Anthropic.Models.Messages;
using AICodeReviewer.Models.Review;

namespace AICodeReviewer.Services.Reviewing
{
    /// <summary>
    /// Asks a Claude model for JSON through the Anthropic SDK's structured outputs
    /// </summary>
    public class AnthropicModelClient(AnthropicClient client, ModelInfo model) : IModelClient
    {
        public async Task<ModelAnswer> CompleteJsonAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            var response = await client.Messages.Create(BuildParams(model, request), cancellationToken: cancellationToken);

            var json = string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
            return new ModelAnswer(
                json,
                (int)response.Usage.InputTokens,
                (int)response.Usage.OutputTokens,
                response.StopReason?.ToString());
        }

        /// <summary>
        /// The request as sent; no temperature, which current Claude models refuse
        /// </summary>
        public static MessageCreateParams BuildParams(ModelInfo model, ModelRequest request)
        {
            var schema = request.Schema.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
            var outputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = schema } };
            if (!string.IsNullOrEmpty(model.Effort))
            {
                outputConfig = outputConfig with
                {
                    Effort = model.Effort switch
                    {
                        "low" => Effort.Low,
                        "medium" => Effort.Medium,
                        "high" => Effort.High,
                        "max" => Effort.Max,
                        _ => throw new InvalidOperationException($"Unknown effort '{model.Effort}' for model '{model.Key}'")
                    }
                };
            }

            return new MessageCreateParams
            {
                Model = model.Id,
                MaxTokens = request.MaxTokens,
                System = request.System,
                Messages = [new UserMessageParam { Content = request.User }],
                OutputConfig = outputConfig,
            };
        }
    }
}
