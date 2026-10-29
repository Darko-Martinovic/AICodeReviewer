using AICodeReviewer.Lab;
using AICodeReviewer.Services;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Tests;

public class LabPromptTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"lab-prompts-{Guid.NewGuid():N}");
    private readonly Dataset _dataset;
    private readonly PromptManagementService _prompts;

    public LabPromptTests()
    {
        var caseFolder = Path.Combine(_folder, "v1", "cases", "a-buggy");
        Directory.CreateDirectory(Path.Combine(caseFolder, "after"));
        File.WriteAllText(Path.Combine(caseFolder, "after", "Repo.cs"), "class Repo { }\n");
        File.WriteAllText(Path.Combine(caseFolder, "labels.json"), "[]");
        File.WriteAllText(Path.Combine(caseFolder, "case.json"),
            """{"id": "a-buggy", "language": "csharp", "file": "Repo.cs", "group": "realistic", "description": "d", "kind": "buggy"}""");
        _dataset = Dataset.Load(Path.Combine(_folder, "v1"));
        _prompts = new PromptManagementService(new LanguageDetectionService(), new FakeConfiguration(), PromptStore.LoadDefault());
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Results => Path.Combine(_folder, "results");

    /// <summary>
    /// Saves a run of one C# case with the given prompt version: found of 10 labels, and false alarms of 10 findings
    /// </summary>
    private void SaveRun(int version, int found, int falseAlarms, string started, string language = "CSharp", string model = "claude-haiku-5-5",
        string caseId = "a-buggy")
    {
        var labels = Enumerable.Range(1, 10).Select(i => $"{caseId}#{i}").ToList();
        var findings = Enumerable.Range(0, 10)
            .Select(i => new ScoredFinding(1, 1, "Bug", "Low", "t", "e", i < falseAlarms ? Scoring.FalseAlarm : Scoring.Real, [], "r"))
            .ToList();
        var caseResult = new CaseResult(caseId, "realistic", "buggy", language, version, 1, "hash", labels, findings,
            labels.Take(found).ToList(), labels.Skip(found).ToList(), 0, 100, 50, 0.001m, 200, 100, 0.002m, 1.0, null);
        var cases = new List<CaseResult> { caseResult };
        // A run from before prompt versions recorded no languages and no versions
        var prompts = language is null ? null! : new Dictionary<string, int> { [language] = version };
        var run = new RunResult("v1", model, "claude-sonnet-5-5", prompts, 1, 3,
            DateTimeOffset.Parse(started), 1.0, false, new Estimate(1, 1, 1, 0m, 0m), Metrics.Of(cases),
            new Dictionary<string, Metrics> { ["realistic"] = Metrics.Of(cases) }, cases);
        Runner.Save(run, Results);
    }

    private PromptLab Lab() => new(Results, _dataset, _prompts);

    [Fact]
    public void A_version_with_better_F1_and_no_loss_of_precision_is_kept()
    {
        SaveRun(version: 1, found: 8, falseAlarms: 5, started: "2026-10-29T10:00:00Z");
        SaveRun(version: 2, found: 8, falseAlarms: 2, started: "2026-10-29T11:00:00Z");

        var verdict = Lab().Decide("CSharp", candidate: 2, baseline: 1, "claude-haiku-5-5", "claude-sonnet-5-5");

        Assert.True(verdict.Keep, verdict.Reason);
        Assert.Equal((0.5, 0.8), (verdict.Baseline.Precision, verdict.Candidate.Precision));
    }

    [Fact]
    public void A_version_that_loses_recall_is_rolled_back_even_when_F1_rises()
    {
        SaveRun(version: 1, found: 8, falseAlarms: 6, started: "2026-10-29T10:00:00Z");
        SaveRun(version: 2, found: 7, falseAlarms: 0, started: "2026-10-29T11:00:00Z");

        var verdict = Lab().Decide("CSharp", 2, 1, "claude-haiku-5-5", "claude-sonnet-5-5");

        Assert.False(verdict.Keep);
        Assert.Contains("recall fell from 80% to 70%", verdict.Reason);
    }

    [Fact]
    public void A_change_within_the_margin_is_rolled_back()
    {
        SaveRun(version: 1, found: 8, falseAlarms: 2, started: "2026-10-29T10:00:00Z");
        SaveRun(version: 2, found: 8, falseAlarms: 2, started: "2026-10-29T11:00:00Z");

        var verdict = Lab().Decide("CSharp", 2, 1, "claude-haiku-5-5", "claude-sonnet-5-5");

        Assert.False(verdict.Keep);
        Assert.Contains("is not a rise of at least 0.02", verdict.Reason);
    }

    [Fact]
    public void Runs_with_another_reviewer_are_not_compared_and_a_missing_baseline_is_an_error()
    {
        SaveRun(version: 1, found: 8, falseAlarms: 5, started: "2026-10-29T10:00:00Z", model: "claude-sonnet-5-5");
        SaveRun(version: 2, found: 9, falseAlarms: 1, started: "2026-10-29T11:00:00Z");

        var error = Assert.Throws<InvalidOperationException>(() => Lab().Decide("CSharp", 2, 1, "claude-haiku-5-5", "claude-sonnet-5-5"));

        Assert.Contains("version 1", error.Message);
    }

    [Fact]
    public void Only_cases_both_versions_reviewed_are_compared()
    {
        SaveRun(version: 1, found: 8, falseAlarms: 5, started: "2026-10-29T10:00:00Z");
        // A case only the baseline reviewed, which would drag the baseline down if it were counted
        SaveRun(version: 1, found: 0, falseAlarms: 10, started: "2026-10-29T10:30:00Z", caseId: "z-only-baseline");
        SaveRun(version: 2, found: 8, falseAlarms: 5, started: "2026-10-29T11:00:00Z");

        var verdict = Lab().Decide("CSharp", 2, 1, "claude-haiku-5-5", "claude-sonnet-5-5");

        Assert.Equal(verdict.Baseline, verdict.Candidate with { });
        Assert.False(verdict.Keep);
        Assert.Single(verdict.BaselineRuns);
    }

    [Fact]
    public void Runs_from_before_prompt_versions_count_as_version_one_of_the_case_language()
    {
        SaveRun(version: 0, found: 8, falseAlarms: 5, started: "2026-10-29T10:00:00Z", language: null!);
        SaveRun(version: 2, found: 9, falseAlarms: 1, started: "2026-10-29T11:00:00Z");

        var board = Lab().Scoreboard();

        Assert.Equal([("CSharp", 1), ("CSharp", 2)], board.Select(s => (s.Language, s.Version)));
        Assert.True(Lab().Decide("CSharp", 2, 1, "claude-haiku-5-5", "claude-sonnet-5-5").Keep);
    }
}
