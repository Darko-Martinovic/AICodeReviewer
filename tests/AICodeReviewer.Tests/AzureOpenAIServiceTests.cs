using System.Net;
using System.Text;
using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Services;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Tests;

public class AzureOpenAIServiceTests
{
    private const string OneIssue = """
        ---
        CATEGORY: Security
        SEVERITY: High
        TITLE: Hardcoded password
        DESCRIPTION: The password is in the source.
        RECOMMENDATION: Read it from configuration.
        LINE: 12
        ---
        """;

    [Fact]
    public async Task The_api_key_goes_on_the_request_not_on_the_shared_client()
    {
        var handler = new RecordingHandler(OneIssue);
        var sharedClient = new HttpClient(handler);
        var service = NewService(sharedClient);

        await service.AnalyzeCodeAsync("Example.cs", "class Example { }");

        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("api-key").Single());
        Assert.False(sharedClient.DefaultRequestHeaders.Contains("api-key"));
    }

    [Fact]
    public async Task An_issue_in_the_answer_becomes_a_detailed_issue()
    {
        var service = NewService(new HttpClient(new RecordingHandler(OneIssue)));

        var (_, detailedIssues, usage) = await service.AnalyzeCodeAsync("Example.cs", "class Example { }");

        var issue = Assert.Single(detailedIssues);
        Assert.Equal("Security", issue.Category);
        Assert.Equal("High", issue.Severity);
        Assert.Equal("Hardcoded password", issue.Title);
        Assert.Equal(12, issue.LineNumber);
        Assert.Equal(150, usage.total_tokens);
    }

    private static AzureOpenAIService NewService(HttpClient client) =>
        new(client, "https://example.openai.azure.com", "test-key", "test-deployment",
            new FakeConfiguration(), new FakePrompts());

    private sealed class RecordingHandler(string answer) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                choices = new[] { new { message = new { role = "assistant", content = answer }, finish_reason = "stop" } },
                usage = new { prompt_tokens = 100, completion_tokens = 50, total_tokens = 150 }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class FakeConfiguration : IConfigurationService
    {
        public AppSettings Settings { get; } = new();
        public void DisplayConfigurationSummary() { }
        public Task SaveSettingsAsync() => Task.CompletedTask;
    }

    private sealed class FakePrompts : IPromptManagementService
    {
        public string GetSystemPrompt(string fileName) => "You review code.";
        public string GetUserPromptTemplate(string fileName) => "{code}";
        public string FormatUserPrompt(string fileName, string fileContent, int contentLimit) => fileContent;
        public bool HasLanguageSpecificPrompts(string fileName) => false;
    }
}
