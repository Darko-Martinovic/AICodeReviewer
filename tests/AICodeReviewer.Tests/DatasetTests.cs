using System.Text.RegularExpressions;
using AICodeReviewer.Lab;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Tests;

/// <summary>
/// Keeps lab/datasets/v1 honest: labels point at real lines, and the files give the reviewer no hints
/// </summary>
public partial class DatasetTests
{
    private static readonly Dataset V1 = Dataset.Load(Dataset.Find("v1", AppContext.BaseDirectory));

    private static readonly Dictionary<string, string[]> Extensions = new()
    {
        ["csharp"] = [".cs"],
        ["typescript"] = [".ts", ".tsx"],
        ["sql"] = [".sql"],
    };

    public static TheoryData<string> CaseIds() => new(V1.Cases.Select(c => c.Info.Id));

    private static DatasetCase Case(string id) => V1.Cases.Single(c => c.Info.Id == id);

    [Fact]
    public void V1_has_at_least_twenty_buggy_and_eight_clean_cases()
    {
        Assert.True(V1.Cases.Count(c => c.Info.Kind == "buggy") >= 20);
        Assert.True(V1.Cases.Count(c => c.Info.Kind == "clean") >= 8);
        Assert.Equal(V1.Cases.Count, V1.Cases.Select(c => c.Info.Id).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void The_case_description_matches_its_files(string id)
    {
        var c = Case(id);

        Assert.Equal(id, Path.GetFileName(c.Folder));
        Assert.Contains(c.Info.Group, new[] { "realistic", "clean", "seeded-dense" });
        Assert.Contains(Path.GetExtension(c.Info.File), Extensions[c.Info.Language]);
        Assert.False(string.IsNullOrWhiteSpace(c.ForReview().Content));
        Assert.Equal(c.Labels().Count > 0 ? "buggy" : "clean", c.Info.Kind);
        Assert.Equal(c.Info.Kind == "clean", c.Info.Group == "clean");
    }

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void Every_label_points_at_lines_in_the_reviewed_file(string id)
    {
        var c = Case(id);
        var lineCount = c.ForReview().Content.TrimEnd('\n').Split('\n').Length;

        foreach (var label in c.Labels())
        {
            Assert.StartsWith($"{id}#", label.Id);
            Assert.InRange(label.Line, 1, lineCount);
            Assert.InRange(label.EndLine, label.Line, lineCount);
            Assert.Contains(label.Category, Reviewer.Categories);
            Assert.Contains(label.Severity, Reviewer.Severities);
            Assert.False(string.IsNullOrWhiteSpace(label.Title));
            Assert.False(string.IsNullOrWhiteSpace(label.Description));
        }
        Assert.Equal(c.Labels().Count, c.Labels().Select(l => l.Id).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void The_files_give_the_reviewer_no_hints(string id)
    {
        var c = Case(id);

        foreach (var text in new[] { c.Info.File, c.ForReview().Content, c.Before() ?? "" })
        {
            var hint = Hint().Match(text);
            Assert.False(hint.Success, $"{id}: '{hint.Value}' tells the reviewer where the defects are");
        }
    }

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void A_before_version_differs_from_the_reviewed_file(string id)
    {
        var c = Case(id);

        if (c.Before() is { } before)
        {
            Assert.NotEqual(before, c.ForReview().Content);
        }
    }

    [GeneratedRegex(@"(?i:\b(bugs?|buggy|vulnerab\w*|insecure|intentional\w*|fixme|hack|leaks?|race condition|injection)\b)|\bTODO\b")]
    private static partial Regex Hint();
}
