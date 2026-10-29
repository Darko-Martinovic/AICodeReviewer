namespace AICodeReviewer.Services.Reviewing
{
    /// <summary>
    /// What a change did to a file: the new file's lines it added or changed, and the lines it removed, each placed
    /// before the new line that now follows them (or after the last line, as line count + 1)
    /// </summary>
    public record ChangeView(IReadOnlySet<int> Added, IReadOnlyDictionary<int, IReadOnlyList<string>> RemovedBefore)
    {
        public bool IsEmpty => Added.Count == 0 && RemovedBefore.Count == 0;
    }

    /// <summary>
    /// Which lines of a file a change touched, read from a unified diff (GitHub's patch) or worked out from two versions
    /// </summary>
    public static class LineDiff
    {
        /// <summary>
        /// The new file's line numbers that the patch adds or changes
        /// </summary>
        public static IReadOnlySet<int> ChangedLinesFromPatch(string patch) => FromPatch(patch).Added;

        /// <summary>
        /// For every new-file line that appears in the patch (added or context), its position in the diff:
        /// the count of lines below the first hunk header, which is what GitHub's review comments are placed by
        /// </summary>
        public static IReadOnlyDictionary<int, int> PositionsFromPatch(string patch) =>
            Walk(patch).Where(l => l.NewLine > 0).ToDictionary(l => l.NewLine, l => l.Position);

        /// <summary>
        /// The change a patch describes, removed lines included
        /// </summary>
        public static ChangeView FromPatch(string patch)
        {
            var added = new HashSet<int>();
            var removed = new Dictionary<int, IReadOnlyList<string>>();
            var pending = new List<string>();
            var lastNewLine = 0;
            foreach (var line in Walk(patch))
            {
                if (line.NewLine == 0)
                {
                    pending.Add(line.Text);
                    continue;
                }
                if (pending.Count > 0)
                {
                    removed[line.NewLine] = pending;
                    pending = new List<string>();
                }
                if (line.Added)
                {
                    added.Add(line.NewLine);
                }
                lastNewLine = line.NewLine;
            }
            if (pending.Count > 0)
            {
                removed[lastNewLine + 1] = pending;
            }
            return new ChangeView(added, removed);
        }

        /// <summary>
        /// One line of a patch: its new-file line number (0 for a removed line), its position and its text
        /// </summary>
        private record PatchLine(int NewLine, int Position, bool Added, string Text);

        private static IEnumerable<PatchLine> Walk(string patch)
        {
            var newLine = 0;
            var position = -1;
            foreach (var line in patch.Replace("\r\n", "\n").Split('\n'))
            {
                if (line.StartsWith("@@", StringComparison.Ordinal))
                {
                    // "@@ -a,b +c,d @@": the new side starts at line c; later headers count as diff lines
                    position++;
                    var plus = line.IndexOf('+');
                    var digits = new string(line.Skip(plus + 1).TakeWhile(char.IsDigit).ToArray());
                    newLine = int.TryParse(digits, out var start) ? start : 1;
                    continue;
                }
                if (position < 0 || line.StartsWith('\\'))
                {
                    continue;
                }

                position++;
                var text = line.Length > 0 ? line[1..] : "";
                if (line.StartsWith('+'))
                {
                    yield return new PatchLine(newLine++, position, true, text);
                }
                else if (line.StartsWith('-'))
                {
                    yield return new PatchLine(0, position, false, text);
                }
                else
                {
                    yield return new PatchLine(newLine++, position, false, text);
                }
            }
        }

        /// <summary>
        /// The change from one version of a file to the next, by longest common subsequence of lines
        /// </summary>
        public static ChangeView Between(string before, string after)
        {
            var a = before.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            var b = after.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            var lcs = new int[a.Length + 1, b.Length + 1];
            for (var i = a.Length - 1; i >= 0; i--)
            {
                for (var j = b.Length - 1; j >= 0; j--)
                {
                    lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                }
            }

            var added = new HashSet<int>();
            var removed = new Dictionary<int, IReadOnlyList<string>>();
            var pending = new List<string>();
            int x = 0, y = 0;
            while (x < a.Length || y < b.Length)
            {
                if (x < a.Length && y < b.Length && a[x] == b[y])
                {
                    Flush(y + 1);
                    x++;
                    y++;
                }
                else if (x < a.Length && (y >= b.Length || lcs[x + 1, y] >= lcs[x, y + 1]))
                {
                    pending.Add(a[x]);
                    x++;
                }
                else
                {
                    Flush(y + 1);
                    added.Add(y + 1);
                    y++;
                }
            }
            Flush(b.Length + 1);
            return new ChangeView(added, removed);

            void Flush(int beforeLine)
            {
                if (pending.Count > 0)
                {
                    removed[beforeLine] = pending;
                    pending = new List<string>();
                }
            }
        }

        /// <summary>
        /// The lines of the new version that are not in the old one
        /// </summary>
        public static IReadOnlySet<int> ChangedLines(string before, string after) => Between(before, after).Added;
    }
}
