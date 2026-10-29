using AICodeReviewer.Models.Review;
using AICodeReviewer.Services;
using AICodeReviewer.Services.Reviewing;
using Microsoft.Extensions.Logging.Abstractions;

namespace AICodeReviewer.Tests;

public class CodeAnalysisServiceTests
{
    [Fact]
    public async Task Findings_become_the_issues_the_pages_show()
    {
        var review = new ReviewOutput
        {
            Model = "claude-haiku-5-5",
            InputTokens = 1200,
            OutputTokens = 300,
            Cost = 0.00027m,
            Findings =
            [
                new Finding
                {
                    Line = 12, EndLine = 12, Category = "Security", Severity = "High", Title = "Hardcoded password",
                    Explanation = "The password is in the source.", Suggestion = "Read it from configuration."
                }
            ]
        };
        var service = new CodeAnalysisService(new StubReviewer(review), NullLogger<CodeAnalysisService>.Instance);

        var (issues, detailedIssues, usage) = await service.AnalyzeCodeAsync("Example.cs", "class Example { }");

        Assert.Equal("[High] Hardcoded password", Assert.Single(issues));
        var issue = Assert.Single(detailedIssues);
        Assert.Equal(("Example.cs", 12), (issue.FileName, issue.LineNumber));
        Assert.Equal("The password is in the source.", issue.Description);
        Assert.Equal("Read it from configuration.", issue.Recommendation);
        Assert.Equal((1200, 300, 1500, 0.00027m), (usage.prompt_tokens, usage.completion_tokens, usage.total_tokens, usage.cost));
    }

    [Fact]
    public async Task A_failed_review_says_so_instead_of_throwing()
    {
        var service = new CodeAnalysisService(
            new StubReviewer(new ModelAnswerException("claude-haiku-5-5 returned JSON that does not match the schema")),
            NullLogger<CodeAnalysisService>.Instance);

        var (issues, detailedIssues, _) = await service.AnalyzeCodeAsync("Example.cs", "class Example { }");

        Assert.StartsWith("AI analysis failed: claude-haiku-5-5 returned JSON", Assert.Single(issues));
        Assert.Empty(detailedIssues);
    }

    private sealed class StubReviewer : IReviewer
    {
        private readonly ReviewOutput? _review;
        private readonly Exception? _error;

        public StubReviewer(ReviewOutput review) => _review = review;
        public StubReviewer(Exception error) => _error = error;

        public ChangeView? Change { get; private set; }

        public Task<ReviewOutput> ReviewFileAsync(string fileName, string content, string? modelKey = null, ChangeView? change = null, CancellationToken cancellationToken = default)
        {
            Change = change;
            return _error is null ? Task.FromResult(_review!) : Task.FromException<ReviewOutput>(_error);
        }
    }

    [Fact]
    public async Task With_a_patch_the_reviewer_is_told_which_lines_changed()
    {
        var reviewer = new StubReviewer(new ReviewOutput { Model = "claude-haiku-5-5" });
        var service = new CodeAnalysisService(reviewer, NullLogger<CodeAnalysisService>.Instance);

        await service.AnalyzeCodeAsync("Repo.cs", "a\nb\nc\n", "@@ -1,2 +1,3 @@\n a\n+b\n c");
        Assert.Equal(new HashSet<int> { 2 }, reviewer.Change!.Added);

        await service.AnalyzeCodeAsync("Repo.cs", "a\nb\nc\n");
        Assert.Null(reviewer.Change);
    }
}
