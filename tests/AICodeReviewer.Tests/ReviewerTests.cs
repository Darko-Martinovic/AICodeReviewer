using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Tests;

public class ReviewerTests
{
    private const string OneFinding = """
        {"findings": [{"line": 2, "end_line": 3, "category": "Security", "severity": "High",
          "title": "Hardcoded password", "explanation": "The password is in the source.",
          "suggestion": "Read it from configuration."}]}
        """;

    private static (Reviewer Reviewer, FakeModelClient Model, FakeModelClientFactory Factory) NewReviewer(
        ModelAnswer answer, string systemPrompt = "You review code.", params (string, string)[] settings)
    {
        var model = new FakeModelClient(answer);
        var factory = new FakeModelClientFactory(model);
        var reviewer = new Reviewer(
            ModelCatalog.LoadDefault(), factory, new FakePrompts(systemPrompt), new FakeConfiguration(), Settings.From(settings));
        return (reviewer, model, factory);
    }

    [Fact]
    public async Task A_finding_comes_back_with_its_lines_and_cost()
    {
        var (reviewer, _, _) = NewReviewer(new ModelAnswer(OneFinding, 1_000_000, 100_000, "end_turn"));

        var review = await reviewer.ReviewFileAsync("Example.cs", "class Example\n{\n}");

        var finding = Assert.Single(review.Findings);
        Assert.Equal((2, 3), (finding.Line, finding.EndLine));
        Assert.Equal("Security", finding.Category);
        Assert.Equal("High", finding.Severity);
        Assert.Equal("Hardcoded password", finding.Title);
        Assert.Equal("claude-haiku-5-5", review.Model);
        // Claude Haiku 5.5: $0.10 per million input tokens, $0.50 per million output tokens
        Assert.Equal(0.15m, review.Cost);
    }

    [Fact]
    public async Task The_model_sees_numbered_lines_the_schema_and_the_output_instructions()
    {
        var (reviewer, model, _) = NewReviewer(new ModelAnswer("""{"findings": []}""", 10, 5, "end_turn"));

        await reviewer.ReviewFileAsync("Example.cs", "class Example\r\n{\r\n}");

        var request = model.LastRequest!;
        Assert.Equal("1 | class Example\n2 | {\n3 | }", request.User);
        Assert.StartsWith("You review code.", request.System);
        Assert.EndsWith(Reviewer.OutputInstructions, request.System);
        Assert.Equal("review_findings", request.SchemaName);
        Assert.Equal(Reviewer.MaxOutputTokens, request.MaxTokens);
    }

    [Fact]
    public async Task Clean_code_can_have_no_findings()
    {
        var (reviewer, _, _) = NewReviewer(new ModelAnswer("""{"findings": []}""", 10, 5, "end_turn"));

        var review = await reviewer.ReviewFileAsync("Example.cs", "class Example { }");

        Assert.Empty(review.Findings);
    }

    [Fact]
    public async Task Review_Model_picks_another_model_from_the_catalog()
    {
        var (reviewer, _, factory) = NewReviewer(
            new ModelAnswer("""{"findings": []}""", 10, 5, "end_turn"), settings: ("Review:Model", "claude-sonnet-5-5"));

        var review = await reviewer.ReviewFileAsync("Example.cs", "class Example { }");

        Assert.Equal("claude-sonnet-5-5", Assert.Single(factory.Created).Key);
        Assert.Equal("claude-sonnet-5-5", review.Model);
    }

    [Fact]
    public async Task A_language_without_a_prompt_gets_the_general_prompt()
    {
        var (reviewer, model, _) = NewReviewer(new ModelAnswer("""{"findings": []}""", 10, 5, "end_turn"), systemPrompt: "");

        await reviewer.ReviewFileAsync("notes.txt", "hello");

        Assert.StartsWith(GeneralPrompt.Default, model.LastRequest!.System);
    }

    [Theory]
    [InlineData("max_tokens")]
    [InlineData("MaxTokens")]
    [InlineData("length")]
    public async Task An_answer_cut_off_by_the_token_limit_is_an_error(string stopReason)
    {
        var (reviewer, _, _) = NewReviewer(new ModelAnswer("""{"findings": [""", 10, 16000, stopReason));

        await Assert.ThrowsAsync<ModelAnswerException>(() => reviewer.ReviewFileAsync("Example.cs", "class Example { }"));
    }

    [Fact]
    public async Task An_answer_that_is_not_the_schema_is_an_error()
    {
        var (reviewer, _, _) = NewReviewer(new ModelAnswer("No issues found", 10, 5, "end_turn"));

        await Assert.ThrowsAsync<ModelAnswerException>(() => reviewer.ReviewFileAsync("Example.cs", "class Example { }"));
    }
}
