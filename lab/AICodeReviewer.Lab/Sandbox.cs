using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using AICodeReviewer.Models.Review;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Lab
{
    /// <summary>
    /// Which pull request in the sandbox repository holds each case, saved in lab/sandbox/&lt;dataset&gt;.json
    /// </summary>
    public record SandboxMap(string Repo, string Dataset, IReadOnlyDictionary<string, int> PullRequests)
    {
        public static string PathFor(string repoRoot, string dataset) => Path.Combine(repoRoot, "lab", "sandbox", $"{dataset}.json");

        public static SandboxMap Load(string repoRoot, string dataset) =>
            JsonSerializer.Deserialize<SandboxMap>(File.ReadAllText(PathFor(repoRoot, dataset)), AICodeReviewer.Lab.Dataset.Json)
            ?? throw new InvalidOperationException($"No sandbox for {dataset}; run sandbox push first");

        public void Save(string repoRoot)
        {
            var path = PathFor(repoRoot, Dataset);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, Runner.ResultJson) + "\n");
        }
    }

    /// <summary>
    /// A line comment a reviewer left on a sandbox pull request
    /// </summary>
    public record PullRequestComment(string Author, string Path, int? Line, string Body);

    /// <summary>
    /// The sandbox: a repository with one pull request per case, where the base branch holds the case's before
    /// version (or nothing, for a new file) and the pull request brings the reviewed version. Any reviewer that
    /// comments on pull requests, Copilot included, can then be scored with the lab's judge
    /// </summary>
    public class Sandbox(string repo, Action<string>? progress = null)
    {
        /// <summary>
        /// Creates the branches and pull requests, numbering cases so branch names say nothing about them
        /// </summary>
        public SandboxMap Push(Dataset dataset, string workFolder)
        {
            if (Directory.Exists(workFolder))
            {
                Directory.Delete(workFolder, recursive: true);
            }
            Run("gh", $"repo clone {repo} \"{workFolder}\"");
            File.WriteAllText(Path.Combine(workFolder, "README.md"),
                "# Review sandbox\n\nEach pull request here is one case of a labelled review dataset. Please review it as you would any change.\n");
            Run("git", "add README.md", workFolder);
            Run("git", "commit -m \"Start the sandbox\"", workFolder);
            Run("git", "branch -M main", workFolder);
            Run("git", "push -u origin main", workFolder);

            var numbered = dataset.Cases.Select((c, i) => (Case: c, Branch: $"change-{i + 1:00}")).ToList();
            foreach (var (c, branch) in numbered)
            {
                Run("git", $"checkout -q -b base-{branch} main", workFolder);
                if (c.Before() is { } before)
                {
                    File.WriteAllText(Path.Combine(workFolder, c.Info.File), before);
                    Run("git", $"add \"{c.Info.File}\"", workFolder);
                    Run("git", $"commit -q -m \"{c.Info.Description}\"", workFolder);
                }
                Run("git", $"checkout -q -b {branch}", workFolder);
                File.WriteAllText(Path.Combine(workFolder, c.Info.File), c.ForReview().Content);
                Run("git", $"add \"{c.Info.File}\"", workFolder);
                Run("git", $"commit -q -m \"{c.Info.Description}\"", workFolder);
                Run("git", "checkout -q main", workFolder);
            }
            Run("git", "push -q origin --all", workFolder);

            var prs = new Dictionary<string, int>();
            foreach (var (c, branch) in numbered)
            {
                var url = Run("gh", $"pr create --repo {repo} --base base-{branch} --head {branch} " +
                    $"--title \"{c.Info.Description}\" --body \"{c.Info.Description}.\"").Trim();
                prs[c.Info.Id] = int.Parse(url[(url.LastIndexOf('/') + 1)..]);
                progress?.Invoke($"{c.Info.Id}: {url}");
            }
            return new SandboxMap(repo, dataset.Name, prs);
        }

        /// <summary>
        /// The pull request's patch for its one file
        /// </summary>
        public string Patch(int pullRequest)
        {
            var files = JsonNode.Parse(Run("gh", $"api repos/{repo}/pulls/{pullRequest}/files"))!.AsArray();
            return files.Select(f => f?["patch"]?.GetValue<string>()).FirstOrDefault(p => p is not null) ?? "";
        }

        /// <summary>
        /// Posts a review with line comments and a summary body
        /// </summary>
        public void PostReview(int pullRequest, string body, IReadOnlyList<InlineComment> comments)
        {
            var review = new JsonObject
            {
                ["body"] = body,
                ["event"] = "COMMENT",
                ["comments"] = new JsonArray(comments.Select(c => (JsonNode)new JsonObject
                {
                    ["path"] = c.Path,
                    ["position"] = c.Position,
                    ["body"] = c.Body
                }).ToArray())
            };
            var input = Path.GetTempFileName();
            File.WriteAllText(input, review.ToJsonString());
            try
            {
                Run("gh", $"api --method POST repos/{repo}/pulls/{pullRequest}/reviews --input \"{input}\"");
            }
            finally
            {
                File.Delete(input);
            }
        }

        /// <summary>
        /// Asks Copilot to review a pull request; it needs Copilot code review on the account and credits left
        /// </summary>
        public void RequestCopilot(int pullRequest) =>
            Run("gh", $"pr edit {pullRequest} --repo {repo} --add-reviewer @copilot");

        /// <summary>
        /// Every line comment on a pull request
        /// </summary>
        public IReadOnlyList<PullRequestComment> Comments(int pullRequest)
        {
            var comments = JsonNode.Parse(Run("gh", $"api --paginate repos/{repo}/pulls/{pullRequest}/comments"))!.AsArray();
            return comments.Select(c => new PullRequestComment(
                c!["user"]?["login"]?.GetValue<string>() ?? "",
                c["path"]?.GetValue<string>() ?? "",
                c["line"]?.GetValue<int?>() ?? c["original_line"]?.GetValue<int?>(),
                c["body"]?.GetValue<string>() ?? "")).ToList();
        }

        /// <summary>
        /// Turns a reviewer's line comments into findings the judge can grade: the first line of the comment is the
        /// title and the whole comment the explanation. Comments without a line are left out
        /// </summary>
        public static List<Finding> ToFindings(IEnumerable<PullRequestComment> comments, string author) =>
            comments
                .Where(c => string.Equals(c.Author, author, StringComparison.OrdinalIgnoreCase) && c.Line is not null)
                .Select(c =>
                {
                    var title = c.Body.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                    return new Finding
                    {
                        Line = c.Line!.Value,
                        EndLine = c.Line.Value,
                        Category = "Bug",
                        Severity = "Medium",
                        Title = title.Trim('*', ' ', '#'),
                        Explanation = c.Body,
                        Suggestion = ""
                    };
                })
                .ToList();

        private static string Run(string file, string arguments, string? folder = null)
        {
            var start = new ProcessStartInfo(file, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = folder ?? Directory.GetCurrentDirectory()
            };
            using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {file}");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"{file} {arguments} failed: {error.Result.Trim()}");
            }
            return output.Result;
        }
    }
}
