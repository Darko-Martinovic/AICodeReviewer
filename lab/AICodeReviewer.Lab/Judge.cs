using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AICodeReviewer.Models.Review;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Lab
{
    /// <summary>
    /// Grades a review against the answer key with one model call per case: each finding is a match of a label,
    /// a real issue without a label, or a false alarm
    /// </summary>
    public class Judge(ModelCatalog catalog, IModelClientFactory clients)
    {
        public const int MaxOutputTokens = 16000;

        public const string Instructions = """
            You grade a code review against an answer key. You get a source file with its line numbers, the defects
            known to be in it (the labels), and the findings a reviewer reported. Give every finding exactly one verdict:

            - match: the finding describes the same defect as one label. Give that label's id. The wording may differ and
              the lines may be a little off, but it must be the same problem in the same code, not merely the same kind
              of problem somewhere else.
            - real: a genuine defect in this code that no label covers, one a careful senior reviewer would want fixed.
              Style preferences, speculative "consider adding" advice and best practices that do not matter here are not real.
            - false_alarm: the finding is wrong, does not apply to this code, or is only a style or preference remark.

            When a finding touches two labels, match the one it mainly describes. Several findings may match the same
            label. Give a short reason for each verdict, and a verdict for every finding, using the finding numbers given.
            For a verdict other than match, leave label_id empty.
            """;

        public static readonly JsonElement Schema = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "verdicts" },
            ["properties"] = new Dictionary<string, object>
            {
                ["verdicts"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                        ["required"] = new[] { "finding", "verdict", "label_id", "reason" },
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["finding"] = new { type = "integer" },
                            ["verdict"] = new { type = "string", @enum = new[] { Scoring.Match, Scoring.Real, Scoring.FalseAlarm } },
                            ["label_id"] = new { type = "string" },
                            ["reason"] = new { type = "string" },
                        }
                    }
                }
            }
        });

        public record Result(IReadOnlyList<JudgeVerdict> Verdicts, int InputTokens, int OutputTokens, decimal Cost);

        public async Task<Result> JudgeAsync(string modelKey, ReviewFile file, IReadOnlyList<Label> labels, IReadOnlyList<Finding> findings, CancellationToken ct = default)
        {
            if (findings.Count == 0)
            {
                return new Result([], 0, 0, 0m);
            }

            var model = catalog.Get(modelKey);
            var answer = await clients.Create(model).CompleteJsonAsync(BuildRequest(file, labels, findings), ct);
            return new Result(ParseVerdicts(answer, findings.Count), answer.InputTokens, answer.OutputTokens,
                model.CostOf(answer.InputTokens, answer.OutputTokens));
        }

        public static ModelRequest BuildRequest(ReviewFile file, IReadOnlyList<Label> labels, IReadOnlyList<Finding> findings)
        {
            var user = new StringBuilder();
            user.Append("File: ").AppendLine(file.FileName).AppendLine("```").AppendLine(Reviewer.NumberLines(file.Content)).AppendLine("```").AppendLine();

            user.AppendLine("Labels:");
            if (labels.Count == 0)
            {
                user.AppendLine("none; this file has no known defects");
            }
            foreach (var label in labels)
            {
                user.AppendLine($"- {label.Id} (lines {label.Line}-{label.EndLine}, {label.Category}/{label.Severity}): {label.Title}. {label.Description}");
            }

            user.AppendLine().AppendLine("Findings:");
            for (var i = 0; i < findings.Count; i++)
            {
                var f = findings[i];
                user.AppendLine($"[{i}] lines {f.Line}-{f.EndLine}, {f.Category}/{f.Severity}: {f.Title}. {f.Explanation}");
            }

            return new ModelRequest(Instructions, user.ToString(), "judge_verdicts", Schema, MaxOutputTokens);
        }

        public static IReadOnlyList<JudgeVerdict> ParseVerdicts(ModelAnswer answer, int findingCount)
        {
            if (answer.StopReason?.Replace("_", "").ToLowerInvariant() is "maxtokens" or "length")
                throw new ModelAnswerException("The judge ran out of output tokens");

            Verdicts? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<Verdicts>(answer.Json);
            }
            catch (JsonException ex)
            {
                throw new ModelAnswerException("The judge returned JSON that does not match the schema", ex);
            }

            var verdicts = parsed?.Items ?? throw new ModelAnswerException("The judge returned no verdicts");
            var numbers = verdicts.Select(v => v.Finding).Order().ToList();
            if (!numbers.SequenceEqual(Enumerable.Range(0, findingCount)))
            {
                throw new ModelAnswerException(
                    $"The judge gave verdicts for findings [{string.Join(", ", numbers)}] instead of one for each of 0-{findingCount - 1}");
            }
            return verdicts.Select(v => new JudgeVerdict(v.Finding, v.Verdict, v.LabelId, v.Reason)).ToList();
        }

        private sealed class Verdicts
        {
            [JsonPropertyName("verdicts")]
            public List<Item> Items { get; set; } = new();
        }

        private sealed class Item
        {
            [JsonPropertyName("finding")] public int Finding { get; set; }
            [JsonPropertyName("verdict")] public string Verdict { get; set; } = "";
            [JsonPropertyName("label_id")] public string LabelId { get; set; } = "";
            [JsonPropertyName("reason")] public string Reason { get; set; } = "";
        }
    }
}
