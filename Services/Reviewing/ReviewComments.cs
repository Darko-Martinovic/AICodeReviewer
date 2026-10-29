namespace AICodeReviewer.Services.Reviewing
{
    /// <summary>
    /// An issue the review found, as the pull request review needs it
    /// </summary>
    public record ReviewIssue(string File, int? Line, string Severity, string Title, string Suggestion);

    /// <summary>
    /// A comment on one line of a pull request's diff
    /// </summary>
    public record InlineComment(string Path, int Line, int Position, string Body);

    /// <summary>
    /// Splits a review's issues into comments on the diff and the rest, which go in the review's summary.
    /// GitHub only accepts comments on lines that appear in the diff
    /// </summary>
    public static class ReviewComments
    {
        public static (IReadOnlyList<InlineComment> Inline, IReadOnlyList<ReviewIssue> Elsewhere) Place(
            IReadOnlyList<ReviewIssue> issues, IReadOnlyDictionary<string, string> patchesByFile)
        {
            var positions = patchesByFile.ToDictionary(p => p.Key, p => LineDiff.PositionsFromPatch(p.Value));
            var inline = new List<InlineComment>();
            var elsewhere = new List<ReviewIssue>();

            foreach (var issue in issues)
            {
                if (issue.Line is { } line
                    && positions.TryGetValue(issue.File, out var filePositions)
                    && filePositions.TryGetValue(line, out var position))
                {
                    inline.Add(new InlineComment(issue.File, line, position, Body(issue)));
                }
                else
                {
                    elsewhere.Add(issue);
                }
            }
            return (inline, elsewhere);
        }

        public static string Body(ReviewIssue issue) =>
            string.IsNullOrWhiteSpace(issue.Suggestion)
                ? $"**{issue.Severity}**: {issue.Title}"
                : $"**{issue.Severity}**: {issue.Title}\n\n{issue.Suggestion}";
    }
}
