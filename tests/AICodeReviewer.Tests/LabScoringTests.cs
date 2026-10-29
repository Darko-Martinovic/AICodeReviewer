using AICodeReviewer.Lab;
using AICodeReviewer.Models.Review;

namespace AICodeReviewer.Tests;

public class LabScoringTests
{
    private static readonly Label Injection = new("c#1", 10, 10, "Security", "Critical", "SQL injection", "...");
    private static readonly Label Leak = new("c#2", 20, 22, "Bug", "Medium", "Connection not disposed", "...");

    private static Finding At(int line, int endLine = 0) => new()
    {
        Line = line, EndLine = endLine == 0 ? line : endLine, Category = "Security", Severity = "High", Title = "t", Explanation = "e"
    };

    [Fact]
    public void A_match_within_the_window_finds_the_label_and_a_second_match_is_a_duplicate()
    {
        var score = Scoring.Score([Injection, Leak], [At(12), At(9)],
            [new(0, "match", "c#1", "same"), new(1, "match", "c#1", "same again")], tolerance: 3);

        Assert.Equal(["c#1"], score.Found);
        Assert.Equal(["c#2"], score.Missed);
        Assert.Equal(["match", "duplicate"], score.Findings.Select(f => f.Verdict));
        Assert.All(score.Findings, f => Assert.Equal("c#1", f.LabelId));
    }

    [Fact]
    public void A_match_outside_the_window_counts_as_real_not_found()
    {
        var score = Scoring.Score([Injection], [At(30)], [new(0, "match", "c#1", "same")], tolerance: 3);

        Assert.Empty(score.Found);
        Assert.Equal("real", Assert.Single(score.Findings).Verdict);
        Assert.Equal(1, score.OutsideWindow);
    }

    [Fact]
    public void A_finding_spanning_the_label_matches_even_when_it_starts_earlier()
    {
        Assert.True(Scoring.Overlaps(At(15, 21), Leak, tolerance: 0));
        Assert.True(Scoring.Overlaps(At(25), Leak, tolerance: 3));
        Assert.False(Scoring.Overlaps(At(26), Leak, tolerance: 3));
    }

    [Fact]
    public void Unknown_labels_unknown_verdicts_and_missing_verdicts_are_not_counted_as_found()
    {
        var score = Scoring.Score([Injection], [At(10), At(10), At(10)],
            [new(0, "match", "c#9", "?"), new(1, "maybe", "", "?")], tolerance: 3);

        Assert.Empty(score.Found);
        Assert.Equal(["real", "false_alarm", "false_alarm"], score.Findings.Select(f => f.Verdict));
    }

    [Fact]
    public void Metrics_count_real_issues_as_good_findings_and_false_alarms_against_precision()
    {
        var buggy = Case("buggy", "realistic", ["c#1", "c#2"], ["c#1"],
            [Scored("match"), Scored("real"), Scored("false_alarm"), Scored("duplicate")]);
        var clean = Case("clean", "clean", [], [], [Scored("false_alarm"), Scored("false_alarm")]);

        var m = Metrics.Of([buggy, clean]);

        Assert.Equal((2, 1, 6, 3), (m.Labels, m.Found, m.Findings, m.FalseAlarms));
        Assert.Equal(0.5, m.Recall);
        Assert.Equal(0.5, m.Precision);
        Assert.Equal(0.3333, m.StrictPrecision);
        Assert.Equal(0.5, m.F1);
        Assert.Equal(2.0, m.FalseAlarmsPerCleanCase);
        Assert.Equal(0.03m, m.ReviewerCost);
    }

    private static ScoredFinding Scored(string verdict) => new(1, 1, "Bug", "Low", "t", "e", verdict, null, "r");

    private static CaseResult Case(string kind, string group, string[] labels, string[] found, ScoredFinding[] findings) =>
        new($"{kind}-case", group, kind, 1, "hash", labels, findings, found, labels.Except(found).ToList(), 0,
            100, 50, 0.015m, 200, 100, 0.01m, 1.0, null);
}
