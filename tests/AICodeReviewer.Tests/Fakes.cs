using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Models.Review;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Services.Reviewing;
using Microsoft.Extensions.Configuration;

namespace AICodeReviewer.Tests;

/// <summary>
/// A model that returns a scripted answer and remembers what it was asked
/// </summary>
public sealed class FakeModelClient(ModelAnswer answer) : IModelClient
{
    public ModelRequest? LastRequest { get; private set; }

    public Task<ModelAnswer> CompleteJsonAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult(answer);
    }
}

public sealed class FakeModelClientFactory(IModelClient client) : IModelClientFactory
{
    public List<ModelInfo> Created { get; } = new();

    public IModelClient Create(ModelInfo model)
    {
        Created.Add(model);
        return client;
    }
}

public sealed class FakeConfiguration : IConfigurationService
{
    public AppSettings Settings { get; } = new();
    public void DisplayConfigurationSummary() { }
    public Task SaveSettingsAsync() => Task.CompletedTask;
}

public sealed class FakePrompts(string systemPrompt = "You review code.") : IPromptManagementService
{
    public string GetSystemPrompt(string fileName) => systemPrompt;
    public string PromptLanguage(string fileName) => "CSharp";
    public string GetUserPromptTemplate(string fileName) => "{fileContent}";
    public string FormatUserPrompt(string fileName, string fileContent, int contentLimit) => fileContent;
    public bool HasLanguageSpecificPrompts(string fileName) => false;
}

public static class Settings
{
    public static IConfiguration From(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
}
