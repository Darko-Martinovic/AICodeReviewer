using AICodeReviewer.Lab;

namespace AICodeReviewer.Tests;

public class LabSandboxTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"sandbox-map-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Only_the_chosen_authors_line_comments_become_findings()
    {
        var comments = new[]
        {
            new PullRequestComment("Copilot", "Repo.cs", 12, "**Possible SQL injection**\n\nThe id is concatenated into the query."),
            new PullRequestComment("copilot", "Repo.cs", 20, "Connection is never disposed"),
            new PullRequestComment("Copilot", "Repo.cs", null, "A comment on the whole file"),
            new PullRequestComment("someone-else", "Repo.cs", 5, "Looks fine to me"),
        };

        var findings = Sandbox.ToFindings(comments, "Copilot");

        Assert.Equal(2, findings.Count);
        Assert.Equal((12, 12), (findings[0].Line, findings[0].EndLine));
        Assert.Equal("Possible SQL injection", findings[0].Title);
        Assert.Contains("concatenated", findings[0].Explanation);
        Assert.Equal("Connection is never disposed", findings[1].Title);
    }

    [Fact]
    public void The_sandbox_map_is_saved_and_read_back()
    {
        new SandboxMap("me/sandbox", "v1", new Dictionary<string, int> { ["cs-01"] = 1, ["sql-02"] = 15 }).Save(_root);

        var map = SandboxMap.Load(_root, "v1");

        Assert.Equal("me/sandbox", map.Repo);
        Assert.Equal(15, map.PullRequests["sql-02"]);
        Assert.True(File.Exists(Path.Combine(_root, "lab", "sandbox", "v1.json")));
    }
}
