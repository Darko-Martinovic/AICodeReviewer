using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AICodeReviewer.Tests;

/// <summary>
/// The Lab page's endpoints, reading a results folder written here instead of the repository's
/// </summary>
public class LabApiTests : IDisposable
{
    private readonly string _results = Path.Combine(Path.GetTempPath(), $"lab-api-{Guid.NewGuid():N}");
    private readonly WebApplicationFactory<Program> _api;
    private readonly HttpClient _client;

    public LabApiTests()
    {
        Directory.CreateDirectory(Path.Combine(_results, "v1"));
        File.WriteAllText(Path.Combine(_results, "scoreboard-v1.json"),
            """{"dataset": "v1", "rows": [{"language": "CSharp", "version": 3, "active": true}]}""");
        File.WriteAllText(Path.Combine(_results, "v1", "20261029-120000-claude-haiku-5-5.json"),
            """{"dataset": "v1", "model": "claude-haiku-5-5", "summary": {"recall": 0.9}, "cases": [{"id": "cs-01"}]}""");
        File.WriteAllText(Path.Combine(_results, "v1", "20261029-130000-claude-haiku-5-5.json"),
            """{"dataset": "v1", "model": "claude-haiku-5-5", "summary": {"recall": 0.95}, "cases": [{"id": "cs-01"}]}""");

        _api = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("GitHub:Token", "test-token");
            builder.UseSetting("GitHub:Owner", "test-owner");
            builder.UseSetting("GitHub:Name", "test-repo");
            builder.UseSetting("Lab:ResultsPath", _results);
        });
        _client = _api.CreateClient();
    }

    public void Dispose()
    {
        _api.Dispose();
        Directory.Delete(_results, recursive: true);
    }

    private async Task<JsonElement> Get(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    [Fact]
    public async Task The_scoreboard_comes_with_the_active_prompt_versions_keyed_as_written()
    {
        var body = await Get("/api/v1/lab/v1/scoreboard");

        Assert.Equal(3, body.GetProperty("scoreboard").GetProperty("rows")[0].GetProperty("version").GetInt32());
        Assert.True(body.GetProperty("active").GetProperty("CSharp").GetInt32() >= 1);
    }

    [Fact]
    public async Task Runs_are_listed_newest_first_without_their_cases()
    {
        var runs = await Get("/api/v1/lab/v1/runs");

        Assert.Equal(2, runs.GetArrayLength());
        Assert.Equal("20261029-130000-claude-haiku-5-5.json", runs[0].GetProperty("file").GetString());
        Assert.Equal(0.95, runs[0].GetProperty("summary").GetProperty("recall").GetDouble());
        Assert.False(runs[0].TryGetProperty("cases", out _));
    }

    [Fact]
    public async Task One_run_comes_back_in_full()
    {
        var run = await Get("/api/v1/lab/v1/runs/20261029-120000-claude-haiku-5-5.json");

        Assert.Equal("cs-01", run.GetProperty("cases")[0].GetProperty("id").GetString());
    }

    [Theory]
    [InlineData("/api/v1/lab/v1/runs/..%2F..%2Fappsettings.json")]
    [InlineData("/api/v1/lab/v1/runs/notes.txt")]
    [InlineData("/api/v1/lab/..%2Fv1/runs")]
    public async Task Paths_outside_the_results_folder_are_refused(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound, $"{path} gave {response.StatusCode}");
    }

    [Fact]
    public async Task A_dataset_without_results_has_no_runs()
    {
        var runs = await Get("/api/v1/lab/v2/runs");

        Assert.Equal(0, runs.GetArrayLength());
    }
}
