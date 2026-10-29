using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AICodeReviewer.Models.Review;

namespace AICodeReviewer.Services.Reviewing
{
    /// <summary>
    /// Asks a model for JSON through a chat completions API with a JSON schema response format:
    /// Azure OpenAI (api-key header) or an OpenAI-compatible API such as OpenRouter or Ollama (bearer token)
    /// </summary>
    public class ChatCompletionsModelClient(HttpClient httpClient, ModelInfo model, string url, string? apiKey, bool azure) : IModelClient
    {
        public async Task<ModelAnswer> CompleteJsonAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(BuildBody(model, request, azure).ToJsonString(), Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrEmpty(apiKey))
            {
                if (azure)
                    message.Headers.Add("api-key", apiKey);
                else
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            using var response = await httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new ModelAnswerException($"{model.Key} answered {(int)response.StatusCode}: {body}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var choice = root.GetProperty("choices")[0];
            var hasUsage = root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object;
            return new ModelAnswer(
                choice.GetProperty("message").GetProperty("content").GetString() ?? "",
                hasUsage ? usage.GetProperty("prompt_tokens").GetInt32() : 0,
                hasUsage ? usage.GetProperty("completion_tokens").GetInt32() : 0,
                choice.TryGetProperty("finish_reason", out var reason) ? reason.GetString() : null);
        }

        public static JsonObject BuildBody(ModelInfo model, ModelRequest request, bool azure)
        {
            var body = new JsonObject
            {
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "system", ["content"] = request.System },
                    new JsonObject { ["role"] = "user", ["content"] = request.User }
                },
                ["max_tokens"] = request.MaxTokens,
                ["response_format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["json_schema"] = new JsonObject
                    {
                        ["name"] = request.SchemaName,
                        ["strict"] = true,
                        ["schema"] = JsonNode.Parse(request.Schema.GetRawText())
                    }
                }
            };
            // Azure names the model in the URL (the deployment); the others name it in the body
            if (!azure)
                body["model"] = model.Id;
            if (model.Temperature is { } temperature)
                body["temperature"] = temperature;
            return body;
        }
    }
}
