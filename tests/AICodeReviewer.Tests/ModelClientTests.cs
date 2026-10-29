using System.Net;
using System.Text;
using System.Text.Json;
using AICodeReviewer.Models.Review;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Tests;

public class ModelClientTests
{
    private static readonly ModelRequest Request = new("You review code.", "1 | class A { }", "review_findings", Reviewer.Schema, 16000);

    [Fact]
    public async Task Azure_gets_the_key_header_the_deployment_url_and_a_strict_schema()
    {
        var handler = new RecordingHandler(ChatAnswer("""{"findings": []}""", 120, 30));
        var factory = new ModelClientFactory(
            Settings.From(
                ("AzureOpenAI:Endpoint", "https://example.openai.azure.com/"),
                ("AzureOpenAI:ApiKey", "azure-key"),
                ("AzureOpenAI:DeploymentName", "reviewer")),
            new HttpClient(handler));

        var answer = await factory.Create(ModelCatalog.LoadDefault().Get("azure-openai")).CompleteJsonAsync(Request);

        Assert.Equal(
            "https://example.openai.azure.com/openai/deployments/reviewer/chat/completions?api-version=2024-10-21",
            handler.Request!.RequestUri!.ToString());
        Assert.Equal("azure-key", handler.Request.Headers.GetValues("api-key").Single());
        using var body = JsonDocument.Parse(handler.Body!);
        var format = body.RootElement.GetProperty("response_format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        Assert.True(format.GetProperty("json_schema").GetProperty("strict").GetBoolean());
        Assert.False(body.RootElement.TryGetProperty("model", out _));
        Assert.Equal(new ModelAnswer("""{"findings": []}""", 120, 30, "stop"), answer);
    }

    [Fact]
    public async Task An_openai_compatible_api_gets_a_bearer_token_and_the_model_in_the_body()
    {
        var handler = new RecordingHandler(ChatAnswer("""{"findings": []}""", 10, 5));
        var model = new ModelInfo
        {
            Key = "router", Provider = "openai-compatible", Id = "some/model",
            BaseUrl = "https://router.example/api/v1/", ApiKeySetting = "Router:ApiKey", Temperature = 0
        };
        var factory = new ModelClientFactory(Settings.From(("Router:ApiKey", "router-key")), new HttpClient(handler));

        await factory.Create(model).CompleteJsonAsync(Request);

        Assert.Equal("https://router.example/api/v1/chat/completions", handler.Request!.RequestUri!.ToString());
        Assert.Equal("Bearer router-key", handler.Request.Headers.Authorization!.ToString());
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("some/model", body.RootElement.GetProperty("model").GetString());
        Assert.Equal(0, body.RootElement.GetProperty("temperature").GetDouble());
    }

    [Fact]
    public async Task A_refused_request_is_an_error_that_names_the_model()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("bad key") });
        var model = new ModelInfo { Key = "local", Provider = "openai-compatible", Id = "m", BaseUrl = "http://localhost:11434/v1" };
        var factory = new ModelClientFactory(Settings.From(), new HttpClient(handler));

        var error = await Assert.ThrowsAsync<ModelAnswerException>(() => factory.Create(model).CompleteJsonAsync(Request));

        Assert.Contains("local answered 401", error.Message);
    }

    [Fact]
    public void Azure_without_an_endpoint_says_which_setting_is_missing()
    {
        var factory = new ModelClientFactory(Settings.From(), new HttpClient());

        var error = Assert.Throws<InvalidOperationException>(() => factory.Create(ModelCatalog.LoadDefault().Get("azure-openai")));

        Assert.Equal("AzureOpenAI:Endpoint is not set", error.Message);
    }

    [Fact]
    public void A_claude_model_gets_the_anthropic_client()
    {
        var factory = new ModelClientFactory(Settings.From(("Anthropic:ApiKey", "test-key")), new HttpClient());

        Assert.IsType<AnthropicModelClient>(factory.Create(ModelCatalog.LoadDefault().Get("claude-haiku-5-5")));
    }

    [Fact]
    public void The_anthropic_request_carries_the_schema_and_the_effort()
    {
        var model = new ModelInfo { Key = "claude-haiku-5-5", Provider = "anthropic", Id = "claude-haiku-5-5", Effort = "low" };

        var parameters = AnthropicModelClient.BuildParams(model, Request);

        Assert.Contains("claude-haiku-5-5", parameters.Model.ToString());
        Assert.Equal(16000, parameters.MaxTokens);
        Assert.NotNull(parameters.OutputConfig?.Format);
        Assert.NotNull(parameters.OutputConfig?.Effort);
    }

    [Fact]
    public void An_unknown_effort_is_refused()
    {
        var model = new ModelInfo { Key = "claude-haiku-5-5", Provider = "anthropic", Id = "claude-haiku-5-5", Effort = "huge" };

        Assert.Throws<InvalidOperationException>(() => AnthropicModelClient.BuildParams(model, Request));
    }

    private static HttpResponseMessage ChatAnswer(string content, int input, int output) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    choices = new[] { new { message = new { role = "assistant", content }, finish_reason = "stop" } },
                    usage = new { prompt_tokens = input, completion_tokens = output, total_tokens = input + output }
                }),
                Encoding.UTF8,
                "application/json")
        };

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return response;
        }
    }
}
