using System.Text.Json;
using AICodeReviewer.Lab;
using AICodeReviewer.Models.Review;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Tests;

public class LabRunnerTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"lab-{Guid.NewGuid():N}");

    public LabRunnerTests()
    {
        WriteCase("a-buggy", "Repo.cs", "class Repo\n{\n    string Sql(string id) => \"SELECT * FROM T WHERE Id = \" + id;\n}\n",
            """[{"id": "a-buggy#1", "line": 3, "end_line": 3, "category": "Security", "severity": "Critical", "title": "SQL injection", "description": "id is concatenated"}]""");
        WriteCase("b-clean", "Slug.cs", "static class Slug\n{\n    public static string Of(string s) => s.Trim().ToLowerInvariant();\n}\n", "[]");
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private void WriteCase(string id, string file, string content, string labels)
    {
        var folder = Path.Combine(_folder, "v1", "cases", id);
        Directory.CreateDirectory(Path.Combine(folder, "after"));
        File.WriteAllText(Path.Combine(folder, "after", file), content);
        File.WriteAllText(Path.Combine(folder, "labels.json"), labels);
        var kind = labels == "[]" ? "clean" : "buggy";
        File.WriteAllText(Path.Combine(folder, "case.json"),
            $$"""{"id": "{{id}}", "language": "csharp", "file": "{{file}}", "group": "{{(kind == "clean" ? "clean" : "realistic")}}", "description": "d", "kind": "{{kind}}"}""");
    }

    /// <summary>
    /// Answers review requests with one finding on line 3 and judge requests with a match for the buggy file
    /// and a false alarm for the clean one
    /// </summary>
    private sealed class ScriptedModel(bool failReviews = false) : IModelClient
    {
        public List<ModelRequest> Requests { get; } = new();

        public Task<ModelAnswer> CompleteJsonAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            lock (Requests) Requests.Add(request);
            if (request.SchemaName == "review_findings")
            {
                if (failReviews) throw new ModelAnswerException("the model is down");
                return Task.FromResult(new ModelAnswer(
                    """{"findings": [{"line": 3, "end_line": 3, "category": "Security", "severity": "High", "title": "t", "explanation": "e", "suggestion": "s"}]}""",
                    1000, 500, "end_turn"));
            }
            var verdict = request.User.Contains("Repo.cs")
                ? """{"verdicts": [{"finding": 0, "verdict": "match", "label_id": "a-buggy#1", "reason": "same defect"}]}"""
                : """{"verdicts": [{"finding": 0, "verdict": "false_alarm", "label_id": "", "reason": "nothing wrong"}]}""";
            return Task.FromResult(new ModelAnswer(verdict, 2000, 400, "end_turn"));
        }
    }

    private (Runner Runner, ScriptedModel Model) NewRunner(bool failReviews = false)
    {
        var model = new ScriptedModel(failReviews);
        var factory = new FakeModelClientFactory(model);
        var catalog = ModelCatalog.LoadDefault();
        var prompts = new FakePrompts();
        var reviewer = new Reviewer(catalog, factory, prompts, new FakeConfiguration(), Settings.From());
        return (new Runner(Dataset.Load(Path.Combine(_folder, "v1")), reviewer, new Judge(catalog, factory), catalog, prompts), model);
    }

    private static readonly RunOptions Options = new("claude-haiku-5-5", "claude-sonnet-5-5");

    [Fact]
    public async Task A_run_scores_every_case_and_adds_up_the_cost()
    {
        var (runner, model) = NewRunner();

        var result = await runner.RunAsync(Options);

        var s = result.Summary;
        Assert.Equal((2, 1, 1, 2, 1), (s.Cases, s.Labels, s.Found, s.Findings, s.FalseAlarms));
        Assert.Equal(1.0, s.Recall);
        Assert.Equal(0.5, s.Precision);
        Assert.Equal(1.0, s.FalseAlarmsPerCleanCase);
        // Haiku 5.5: 1,000 in and 500 out per review; Sonnet 5.5: 2,000 in and 400 out per judgement
        Assert.Equal(2 * 0.00035m, s.ReviewerCost);
        Assert.Equal(2 * 0.008m, s.JudgeCost);
        Assert.Equal(["clean", "realistic"], result.Groups.Keys);
        Assert.Equal(4, model.Requests.Count);
        Assert.False(result.StoppedByBudget);
    }

    [Fact]
    public async Task The_judge_sees_the_labels_but_the_reviewer_does_not()
    {
        var (runner, model) = NewRunner();

        await runner.RunAsync(Options with { Cases = "a-" });

        var review = model.Requests.Single(r => r.SchemaName == "review_findings");
        var judge = model.Requests.Single(r => r.SchemaName == "judge_verdicts");
        Assert.DoesNotContain("a-buggy#1", review.System + review.User);
        Assert.DoesNotContain("id is concatenated", review.System + review.User);
        Assert.Contains("a-buggy#1 (lines 3-3, Security/Critical): SQL injection. id is concatenated", judge.User);
        Assert.Contains("[0] lines 3-3, Security/High: t. e", judge.User);
    }

    [Fact]
    public async Task The_budget_stops_the_run_before_a_case_would_go_over_it()
    {
        var (runner, model) = NewRunner();

        var result = await runner.RunAsync(Options with { MaxCost = 0.0001m });

        Assert.True(result.StoppedByBudget);
        Assert.Empty(result.Cases);
        Assert.Empty(model.Requests);
    }

    [Fact]
    public async Task A_failed_review_is_recorded_and_its_labels_count_as_missed()
    {
        var (runner, _) = NewRunner(failReviews: true);

        var result = await runner.RunAsync(Options);

        Assert.Equal(2, result.Summary.Errors);
        Assert.Equal(0, result.Summary.Found);
        Assert.Contains("the model is down", result.Cases[0].Error);
        Assert.Equal(["a-buggy#1"], result.Cases[0].Missed);
    }

    [Fact]
    public async Task The_saved_results_use_snake_case_and_carry_every_case()
    {
        var (runner, _) = NewRunner();
        var result = await runner.RunAsync(Options with { Repeat = 2 });

        var path = Runner.Save(result, Path.Combine(_folder, "results"));

        using var json = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(Path.Combine(_folder, "results", "v1"), Path.GetDirectoryName(path));
        Assert.EndsWith("-claude-haiku-5-5.json", path);
        Assert.Equal(4, json.RootElement.GetProperty("cases").GetArrayLength());
        Assert.Equal(1.0, json.RootElement.GetProperty("summary").GetProperty("recall").GetDouble());
        Assert.True(json.RootElement.GetProperty("estimate").GetProperty("total").GetDecimal() > 0);
    }

    [Fact]
    public void The_estimate_grows_with_the_cases_and_repeats()
    {
        var (runner, _) = NewRunner();

        var one = runner.EstimateCost(Options with { Cases = "a-" });
        var all = runner.EstimateCost(Options);
        var twice = runner.EstimateCost(Options with { Repeat = 2 });

        Assert.Equal((1, 2, 4), (one.Cases, all.Cases, twice.Cases));
        Assert.True(one.Total > 0 && one.Total < all.Total && all.Total < twice.Total);
    }

    [Fact]
    public async Task The_judge_is_not_asked_when_there_are_no_findings()
    {
        var model = new FakeModelClient(new ModelAnswer("{}", 0, 0, "end_turn"));
        var judge = new Judge(ModelCatalog.LoadDefault(), new FakeModelClientFactory(model));

        var result = await judge.JudgeAsync("claude-sonnet-5-5", new ReviewFile("A.cs", "class A { }"), [], []);

        Assert.Empty(result.Verdicts);
        Assert.Null(model.LastRequest);
    }

    [Fact]
    public void A_judge_answer_that_skips_a_finding_is_an_error()
    {
        var answer = new ModelAnswer("""{"verdicts": [{"finding": 0, "verdict": "real", "label_id": "", "reason": "r"}]}""", 1, 1, "end_turn");

        Assert.Throws<ModelAnswerException>(() => Judge.ParseVerdicts(answer, 2));
    }
}
