using System.Text.Json;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Lab
{
    /// <summary>
    /// A saved run and the file it came from
    /// </summary>
    public record SavedRun(string File, RunResult Result);

    /// <summary>
    /// The pooled scores of one prompt version of one language with one reviewer and judge, over every saved run
    /// </summary>
    public record VersionScore(string Language, int Version, string Model, string JudgeModel, int Runs, Metrics Metrics, string Mode = "file");

    public record ScoreboardRow(
        string Language, int Version, bool Active, string Model, string JudgeModel, int Runs, Metrics Metrics, string Mode,
        string Notes, PromptDecision? Decision);

    public record ScoreboardDocument(string Dataset, DateTimeOffset UpdatedAt, IReadOnlyList<ScoreboardRow> Rows);

    /// <summary>
    /// What the lab decided about a candidate version, and the scores it compared
    /// </summary>
    public record Verdict(bool Keep, string Reason, Metrics Baseline, Metrics Candidate, IReadOnlyList<string> BaselineRuns, IReadOnlyList<string> CandidateRuns);

    /// <summary>
    /// Reads the saved runs, scores prompt versions and applies the keep-or-roll-back rule
    /// </summary>
    public class PromptLab(string resultsFolder, Dataset dataset, IPromptManagementService prompts)
    {
        /// <summary>
        /// A candidate is kept only when F1 rises by at least the margin and neither precision nor recall falls by more than it
        /// </summary>
        public const double DefaultMargin = 0.02;

        public IReadOnlyList<SavedRun> Runs()
        {
            var folder = Path.Combine(resultsFolder, dataset.Name);
            if (!Directory.Exists(folder))
            {
                return [];
            }
            return Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal)
                .Select(path => new SavedRun(Path.GetFileName(path), Normalize(
                    JsonSerializer.Deserialize<RunResult>(File.ReadAllText(path), Dataset.Json)
                    ?? throw new InvalidOperationException($"{path} is empty"))))
                .ToList();
        }

        /// <summary>
        /// Fills in what runs from before prompt versions did not record: the case's prompt language, and version 1
        /// </summary>
        private RunResult Normalize(RunResult run)
        {
            var files = dataset.Cases.ToDictionary(c => c.Info.Id, c => c.Info.File);
            var cases = run.Cases.Select(c => c with
            {
                Language = string.IsNullOrEmpty(c.Language) && files.TryGetValue(c.Id, out var file) ? prompts.PromptLanguage(file) : c.Language,
                PromptVersion = c.PromptVersion == 0 ? 1 : c.PromptVersion
            }).ToList();
            var versions = cases.GroupBy(c => c.Language).ToDictionary(g => g.Key, g => g.First().PromptVersion);
            return run with { Cases = cases, Prompts = run.Prompts ?? versions };
        }

        public IReadOnlyList<VersionScore> Scoreboard()
        {
            var cases = Runs().SelectMany(r => r.Result.Cases.Select(c => (Run: r, Case: c)));
            return cases
                .GroupBy(x => (x.Case.Language, x.Case.PromptVersion, x.Run.Result.Model, x.Run.Result.JudgeModel, Mode: x.Run.Result.Mode ?? "file"))
                .OrderBy(g => g.Key.Language, StringComparer.Ordinal).ThenBy(g => g.Key.PromptVersion).ThenBy(g => g.Key.Model, StringComparer.Ordinal).ThenBy(g => g.Key.Mode, StringComparer.Ordinal)
                .Select(g => new VersionScore(g.Key.Language, g.Key.PromptVersion, g.Key.Model, g.Key.JudgeModel,
                    g.Select(x => x.Run.File).Distinct().Count(), Metrics.Of(g.Select(x => x.Case).ToList()), g.Key.Mode))
                .ToList();
        }

        public Verdict Decide(string language, int candidate, int baseline, string model, string judgeModel, double margin = DefaultMargin, string mode = "file")
        {
            List<(string File, List<CaseResult> Cases)> Pool(int version)
            {
                var matching = Runs()
                    .Where(r => r.Result.Model == model && r.Result.JudgeModel == judgeModel && (r.Result.Mode ?? "file") == mode)
                    .Select(r => (r.File, Cases: r.Result.Cases.Where(c => c.Language == language && c.PromptVersion == version).ToList()))
                    .Where(r => r.Cases.Count > 0)
                    .ToList();
                return matching.Count > 0
                    ? matching
                    : throw new InvalidOperationException(
                        $"No saved run reviewed {language} cases with version {version}, {model} and judge {judgeModel}");
            }

            // Both sides are scored on the cases they share, so a run over more files does not tilt the comparison
            var baselinePool = Pool(baseline);
            var candidatePool = Pool(candidate);
            var shared = baselinePool.SelectMany(r => r.Cases).Select(c => c.Id)
                .Intersect(candidatePool.SelectMany(r => r.Cases).Select(c => c.Id)).ToHashSet();
            if (shared.Count == 0)
            {
                throw new InvalidOperationException($"The runs of {language} v{baseline} and v{candidate} share no cases");
            }

            (Metrics Metrics, List<string> Runs) Score(List<(string File, List<CaseResult> Cases)> pool)
            {
                var runs = pool.Where(r => r.Cases.Any(c => shared.Contains(c.Id))).ToList();
                return (Metrics.Of(runs.SelectMany(r => r.Cases).Where(c => shared.Contains(c.Id)).ToList()), runs.Select(r => r.File).ToList());
            }

            var (before, beforeRuns) = Score(baselinePool);
            var (after, afterRuns) = Score(candidatePool);

            // The reason is saved in prompts.json, so it reads the same whatever the machine's culture
            var reasons = new List<string>();
            if (after.F1 < before.F1 + margin) reasons.Add(Invariant($"F1 {before.F1:F2} to {after.F1:F2} is not a rise of at least {margin:F2}"));
            if (after.Precision < before.Precision - margin) reasons.Add($"precision fell from {Percent(before.Precision)} to {Percent(after.Precision)}");
            if (after.Recall < before.Recall - margin) reasons.Add($"recall fell from {Percent(before.Recall)} to {Percent(after.Recall)}");

            var keep = reasons.Count == 0;
            var reason = keep
                ? Invariant($"F1 rose from {before.F1:F2} to {after.F1:F2}") +
                  $" (precision {Percent(before.Precision)} to {Percent(after.Precision)}, recall {Percent(before.Recall)} to {Percent(after.Recall)})"
                : string.Join("; ", reasons);
            return new Verdict(keep, reason, before, after, beforeRuns, afterRuns);
        }

        private static string Invariant(FormattableString text) => FormattableString.Invariant(text);

        private static string Percent(double rate) => Invariant($"{Math.Round(rate * 100):0}%");

        /// <summary>
        /// Writes lab/results/scoreboard-&lt;dataset&gt;.json, the scoreboard with each version's notes and decision,
        /// which the app's Lab page shows. It sits beside the dataset's folder of runs, not in it
        /// </summary>
        public string SaveScoreboard(PromptStore store)
        {
            var rows = Scoreboard().Select(s =>
            {
                var version = store.Versions.FirstOrDefault(v => v.Language == s.Language && v.Version == s.Version);
                return new ScoreboardRow(s.Language, s.Version, store.ActiveVersion(s.Language) == s.Version, s.Model, s.JudgeModel,
                    s.Runs, s.Metrics, s.Mode, version?.Notes ?? "", version?.Decision);
            }).ToList();
            var path = Path.Combine(resultsFolder, $"scoreboard-{dataset.Name}.json");
            Directory.CreateDirectory(resultsFolder);
            File.WriteAllText(path, JsonSerializer.Serialize(new ScoreboardDocument(dataset.Name, DateTimeOffset.UtcNow, rows), Runner.ResultJson) + "\n");
            return path;
        }

        public static PromptDecision ToDecision(Verdict verdict, DateOnly today) => new(
            verdict.Keep ? "kept" : "rolled_back", today.ToString("yyyy-MM-dd"), verdict.Reason,
            verdict.Baseline.Recall, verdict.Candidate.Recall, verdict.Baseline.Precision, verdict.Candidate.Precision,
            verdict.Baseline.F1, verdict.Candidate.F1, verdict.BaselineRuns, verdict.CandidateRuns);
    }
}
