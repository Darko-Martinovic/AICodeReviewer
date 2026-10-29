using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AICodeReviewer.Tests;

/// <summary>
/// Starts the whole API in memory with placeholder settings, so the package upgrades
/// (ASP.NET Core, API versioning, Swashbuckle, Semantic Kernel) are checked end to end
/// </summary>
public class ApiSmokeTests : IClassFixture<ApiSmokeTests.Api>
{
    public class Api : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("AzureOpenAI:Endpoint", "https://example.openai.azure.com");
            builder.UseSetting("AzureOpenAI:ApiKey", "test-key");
            builder.UseSetting("AzureOpenAI:DeploymentName", "test-deployment");
            builder.UseSetting("GitHub:Token", "test-token");
            builder.UseSetting("GitHub:Owner", "test-owner");
            builder.UseSetting("GitHub:Name", "test-repo");
        }
    }

    private readonly HttpClient _client;

    public ApiSmokeTests(Api api) => _client = api.CreateClient();

    [Fact]
    public async Task Health_answers()
    {
        var response = await _client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_versioned_controller_route_answers()
    {
        var response = await _client.GetAsync("/api/v1/systemprompts/templates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_describes_the_versioned_routes()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();

        Assert.Contains("/api/v1/SystemPrompts/templates", paths, StringComparer.OrdinalIgnoreCase);
    }
}
