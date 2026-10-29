using System.Diagnostics;
using System.Text.Json;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Lab
{
    public record RunOptions(
        string Model, string JudgeModel, int Repeat = 1, decimal MaxCost = 2.00m, string? Cases = null,
        int Tolerance = 3, int Parallelism = 4, IReadOnlyCollection<string>? Languages = null);

    public record Estimate(int Cases, long InputTokens, long OutputTokens, decimal ReviewerCost, decimal JudgeCost)
    {
        public decimal Total => ReviewerCost + JudgeCost;
    }

    /// <param name="Prompts">The prompt version each language in the run used</param>
    public record RunResult(
        string Dataset, string Model, string JudgeModel, IReadOnlyDictionary<string, int> Prompts, int Repeat, int Tolerance,
        DateTimeOffset StartedAt, double Seconds, bool StoppedByBudget, Estimate Estimate, Metrics Summary,
        IReadOnlyDictionary<string, Metrics> Groups, IReadOnlyList<CaseResult> Cases);

    /// <summary>
    /// Reviews every case of a dataset with one model, has the judge grade each review and adds up the scores,
    /// stopping before a case would take the spending past the budget
    /// </summary>
    /// <param name="promptStore">The prompt versions the reviewer's prompts come from, to record which version each case used</param>
    public class Runner(Dataset dataset, IReviewer reviewer, Judge judge, ModelCatalog catalog, IPromptManagementService prompts, PromptStore promptStore)
    {
        public IReadOnlyList<DatasetCase> Select(RunOptions options) =>
            dataset.Cases
                .Where(c => options.Cases is null || c.Info.Id.StartsWith(options.Cases, StringComparison.Ordinal))
                .Where(c => options.Languages is null || options.Languages.Contains(prompts.PromptLanguage(c.Info.File)))
                .ToList();

        /// <summary>
        /// A rough estimate from the size of each file: about 3.5 characters a token, and output that grows with the
        /// file, thinking included. Real runs have come in under it; the budget check during the run is what binds
        /// </summary>
        public Estimate EstimateCost(RunOptions options)
        {
            var reviewerModel = catalog.Get(options.Model);
            var judgeModel = catalog.Get(options.JudgeModel);
            long input = 0, output = 0;
            decimal reviewerCost = 0, judgeCost = 0;

            foreach (var c in Select(options))
            {
                var (reviewIn, reviewOut, judgeIn, judgeOut) = CaseTokens(c);
                input += (reviewIn + judgeIn) * options.Repeat;
                output += (reviewOut + judgeOut) * options.Repeat;
                reviewerCost += reviewerModel.CostOf(reviewIn, reviewOut) * options.Repeat;
                judgeCost += judgeModel.CostOf(judgeIn, judgeOut) * options.Repeat;
            }
            return new Estimate(Select(options).Count * options.Repeat, input, output, Math.Round(reviewerCost, 4), Math.Round(judgeCost, 4));
        }

        private (int ReviewIn, int ReviewOut, int JudgeIn, int JudgeOut) CaseTokens(DatasetCase c)
        {
            var file = c.ForReview();
            var numbered = Reviewer.NumberLines(file.Content);
            var lines = numbered.Count(ch => ch == '\n') + 1;
            var findings = Math.Max(2, lines / 10);

            var reviewIn = (int)((SystemPrompt(file.FileName).Length + numbered.Length + 400) / 3.5);
            var reviewOut = 400 + 20 * lines;
            var judgeIn = (int)((Judge.Instructions.Length + numbered.Length + 250 * c.Labels().Count + 300 * findings) / 3.5);
            var judgeOut = 1500 + 200 * findings;
            return (reviewIn, reviewOut, judgeIn, judgeOut);
        }

        public async Task<RunResult> RunAsync(RunOptions options, Action<string>? progress = null, CancellationToken ct = default)
        {
            var startedAt = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var estimate = EstimateCost(options);
            var work = Enumerable.Range(1, options.Repeat).SelectMany(r => Select(options).Select(c => (Case: c, Repeat: r))).ToList();

            var results = new List<CaseResult>();
            var gate = new SemaphoreSlim(options.Parallelism);
            var spentLock = new object();
            decimal spent = 0, committed = 0;
            var stoppedByBudget = false;

            var tasks = work.Select(async item =>
            {
                await gate.WaitAsync(ct);
                try
                {
                    var (reviewIn, reviewOut, judgeIn, judgeOut) = CaseTokens(item.Case);
                    var caseEstimate = catalog.Get(options.Model).CostOf(reviewIn, reviewOut) + catalog.Get(options.JudgeModel).CostOf(judgeIn, judgeOut);
                    lock (spentLock)
                    {
                        if (spent + committed + caseEstimate > options.MaxCost)
                        {
                            stoppedByBudget = true;
                            return;
                        }
                        committed += caseEstimate;
                    }

                    var result = await RunCaseAsync(item.Case, item.Repeat, options, ct);
                    lock (spentLock)
                    {
                        committed -= caseEstimate;
                        spent += result.ReviewerCost + result.JudgeCost;
                        results.Add(result);
                        progress?.Invoke($"{item.Case.Info.Id} (run {item.Repeat}): {result.Found.Count}/{result.Labels.Count} found, " +
                            $"{result.Findings.Count} findings, ${result.ReviewerCost + result.JudgeCost:F4}" +
                            (result.Error is null ? "" : $", error: {result.Error}"));
                    }
                }
                finally
                {
                    gate.Release();
                }
            });
            await Task.WhenAll(tasks);

            var ordered = results.OrderBy(r => r.Repeat).ThenBy(r => r.Id, StringComparer.Ordinal).ToList();
            var groups = ordered.GroupBy(r => r.Group).OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => Metrics.Of(g.ToList()));
            var promptVersions = ordered.GroupBy(r => r.Language).OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().PromptVersion);
            return new RunResult(dataset.Name, options.Model, options.JudgeModel, promptVersions, options.Repeat, options.Tolerance,
                startedAt, Math.Round(stopwatch.Elapsed.TotalSeconds, 1), stoppedByBudget, estimate, Metrics.Of(ordered), groups, ordered);
        }

        private async Task<CaseResult> RunCaseAsync(DatasetCase c, int repeat, RunOptions options, CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();
            var file = c.ForReview();
            var labels = c.Labels();
            var labelIds = labels.Select(l => l.Id).ToList();
            var language = prompts.PromptLanguage(file.FileName);
            var version = promptStore.ActiveVersion(language);
            var promptHash = Hash(SystemPrompt(file.FileName));

            ReviewOutputSummary review = new(0, 0, 0m);
            try
            {
                var output = await reviewer.ReviewFileAsync(file.FileName, file.Content, options.Model, ct);
                review = new ReviewOutputSummary(output.InputTokens, output.OutputTokens, output.Cost);

                var judged = await judge.JudgeAsync(options.JudgeModel, file, labels, output.Findings, ct);
                var score = Scoring.Score(labels, output.Findings, judged.Verdicts, options.Tolerance);
                return new CaseResult(c.Info.Id, c.Info.Group, c.Info.Kind, language, version, repeat, promptHash, labelIds, score.Findings, score.Found, score.Missed,
                    score.OutsideWindow, review.InputTokens, review.OutputTokens, review.Cost,
                    judged.InputTokens, judged.OutputTokens, judged.Cost, Math.Round(stopwatch.Elapsed.TotalSeconds, 1), null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new CaseResult(c.Info.Id, c.Info.Group, c.Info.Kind, language, version, repeat, promptHash, labelIds, [], [], labelIds, 0,
                    review.InputTokens, review.OutputTokens, review.Cost, 0, 0, 0m, Math.Round(stopwatch.Elapsed.TotalSeconds, 1),
                    $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        private record ReviewOutputSummary(int InputTokens, int OutputTokens, decimal Cost);

        private string SystemPrompt(string fileName) => Reviewer.SystemPromptFor(prompts.GetSystemPrompt(fileName));

        private static string Hash(string systemPrompt) => PromptStore.Hash(systemPrompt);

        public static readonly JsonSerializerOptions ResultJson = new(Dataset.Json) { WriteIndented = true };

        /// <summary>
        /// Saves the run as lab/results/&lt;dataset&gt;/&lt;time&gt;-&lt;model&gt;.json and returns the path
        /// </summary>
        public static string Save(RunResult result, string resultsFolder)
        {
            var folder = Path.Combine(resultsFolder, result.Dataset);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"{result.StartedAt:yyyyMMdd-HHmmss}-{result.Model}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(result, ResultJson) + "\n");
            return path;
        }
    }
}
