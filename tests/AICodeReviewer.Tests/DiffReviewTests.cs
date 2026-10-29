using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Tests;

public class DiffReviewTests
{
    // GitHub's patch for a file where line 3 changed and line 6 was added
    private const string Patch = """
        @@ -1,4 +1,4 @@
         class Repo
         {
        -    string Sql(string id) => Query("SELECT * FROM T WHERE Id = @id", id);
        +    string Sql(string id) => Query("SELECT * FROM T WHERE Id = " + id);
         }
        @@ -5,2 +5,3 @@ class Other
         {
        +    int count;
         }
        """;

    [Fact]
    public void Changed_lines_come_from_the_added_lines_of_each_hunk()
    {
        Assert.Equal(new HashSet<int> { 3, 6 }, LineDiff.ChangedLinesFromPatch(Patch));
    }

    [Fact]
    public void Positions_count_lines_below_the_first_hunk_header_including_later_headers()
    {
        var positions = LineDiff.PositionsFromPatch(Patch);

        // 1: class Repo, 2: {, 3: removed line, 4: line 3, 5: }, 6: second header, 7: line 5, 8: added line 6, 9: line 7
        Assert.Equal(1, positions[1]);
        Assert.Equal(4, positions[3]);
        Assert.Equal(5, positions[4]);
        Assert.Equal(7, positions[5]);
        Assert.Equal(8, positions[6]);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, positions.Keys.Order());
    }

    [Fact]
    public void Changed_lines_between_two_versions_are_the_new_and_edited_lines()
    {
        var before = "a\nb\nc\nd\n";
        var after = "a\nB\nc\nd\ne\n";

        Assert.Equal(new HashSet<int> { 2, 5 }, LineDiff.ChangedLines(before, after));
        Assert.Empty(LineDiff.ChangedLines(after, after));
    }

    [Fact]
    public void Removed_lines_are_kept_with_the_line_that_now_follows_them()
    {
        var fromPatch = LineDiff.FromPatch(Patch);
        Assert.Equal(["    string Sql(string id) => Query(\"SELECT * FROM T WHERE Id = @id\", id);"], fromPatch.RemovedBefore[3]);

        var between = LineDiff.Between("a\nguard\nb\nend\n", "a\nb\n");
        Assert.Empty(between.Added);
        Assert.Equal(["guard"], between.RemovedBefore[2]);
        Assert.Equal(["end"], between.RemovedBefore[3]);
    }

    [Fact]
    public void A_removed_line_is_shown_where_it_was_without_a_number()
    {
        var change = LineDiff.Between("if (ok)\nrun();\n", "run();\n");

        Assert.Equal("  - | if (ok)\n1   | run();", Reviewer.NumberLines("run();", change));
    }

    [Fact]
    public void Issues_on_diff_lines_become_line_comments_and_the_rest_stay_in_the_summary()
    {
        var issues = new List<ReviewIssue>
        {
            new("Repo.cs", 3, "Critical", "SQL injection", "Use a parameter."),
            new("Repo.cs", 40, "Low", "Far from the change", ""),
            new("Other.cs", 3, "High", "A file without a patch", ""),
            new("Repo.cs", null, "Medium", "No line", ""),
        };

        var (inline, elsewhere) = ReviewComments.Place(issues, new Dictionary<string, string> { ["Repo.cs"] = Patch });

        var comment = Assert.Single(inline);
        Assert.Equal(("Repo.cs", 3, 4), (comment.Path, comment.Line, comment.Position));
        Assert.Equal("**Critical**: SQL injection\n\nUse a parameter.", comment.Body);
        Assert.Equal(3, elsewhere.Count);
    }

    [Fact]
    public async Task A_change_review_marks_the_changed_lines_and_says_what_to_report()
    {
        var model = new FakeModelClient(new ModelAnswer("""{"findings": []}""", 10, 5, "end_turn"));
        var reviewer = new Reviewer(ModelCatalog.LoadDefault(), new FakeModelClientFactory(model), new FakePrompts(),
            new FakeConfiguration(), Settings.From());

        await reviewer.ReviewFileAsync("Repo.cs", "a\nb\nc", change: new ChangeView(new HashSet<int> { 2 }, new Dictionary<int, IReadOnlyList<string>>()));

        Assert.Equal("1   | a\n2 + | b\n3   | c", model.LastRequest!.User);
        Assert.EndsWith(Reviewer.ChangeInstructions, model.LastRequest.System);
    }

    [Fact]
    public async Task A_change_that_only_deletes_lines_is_not_sent_to_the_model()
    {
        var model = new FakeModelClient(new ModelAnswer("""{"findings": []}""", 10, 5, "end_turn"));
        var reviewer = new Reviewer(ModelCatalog.LoadDefault(), new FakeModelClientFactory(model), new FakePrompts(),
            new FakeConfiguration(), Settings.From());

        var review = await reviewer.ReviewFileAsync("Repo.cs", "a\nb", change: new ChangeView(new HashSet<int>(), new Dictionary<int, IReadOnlyList<string>>()));

        Assert.Empty(review.Findings);
        Assert.Null(model.LastRequest);
    }
}
