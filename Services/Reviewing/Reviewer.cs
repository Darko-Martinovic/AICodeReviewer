using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AICodeReviewer.Models.Review;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Services.Reviewing
{
    /// <summary>
    /// Reviews one file with a model from models.json and returns structured findings
    /// </summary>
    public interface IReviewer
    {
        /// <param name="modelKey">A key from models.json; the configured reviewer when null</param>
        /// <param name="change">For the review of a change, what it added and removed; null reviews the whole file</param>
        Task<ReviewOutput> ReviewFileAsync(string fileName, string content, string? modelKey = null,
            ChangeView? change = null, CancellationToken cancellationToken = default);
    }

    public class Reviewer(
        ModelCatalog catalog,
        IModelClientFactory clients,
        IPromptManagementService prompts,
        IConfigurationService configurationService,
        IConfiguration configuration) : IReviewer
    {
        public const int MaxOutputTokens = 16000;

        public static readonly string[] Categories = ["Security", "Bug", "Performance", "Maintainability", "Quality", "Design"];
        public static readonly string[] Severities = ["Critical", "High", "Medium", "Low"];

        /// <summary>
        /// Appended to every system prompt, including custom ones, so findings point at real lines
        /// </summary>
        public const string OutputInstructions = """
            The file is shown with its line numbers in the left margin.
            Report each issue as one finding. Give the line where the issue starts (line) and where it ends
            (end_line, the same number for a single line), using the numbers in the margin.
            Use one category: Security, Bug, Performance, Maintainability, Quality or Design.
            Use one severity: Critical, High, Medium or Low.
            If you find no real issues, return an empty list of findings.
            """;

        /// <summary>
        /// Appended when the review is of a change rather than a whole file
        /// </summary>
        public const string ChangeInstructions = """
            This review is of a change. Lines marked + in the margin were added or changed. Lines marked - were
            removed: they are shown where they used to be, have no line number and are no longer in the file. The other
            lines are unchanged context. Report only defects that the change introduces: in the + lines, or caused by a
            removal, or caused elsewhere in the file by the change. Give a defect caused by a removal the number of the
            nearest line that is still in the file. Do not report problems the change did not introduce.
            """;

        public static readonly JsonElement Schema = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new[] { "findings" },
            ["properties"] = new Dictionary<string, object>
            {
                ["findings"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                        ["required"] = new[] { "line", "end_line", "category", "severity", "title", "explanation", "suggestion" },
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["line"] = new { type = "integer" },
                            ["end_line"] = new { type = "integer" },
                            ["category"] = new { type = "string", @enum = Categories },
                            ["severity"] = new { type = "string", @enum = Severities },
                            ["title"] = new { type = "string" },
                            ["explanation"] = new { type = "string" },
                            ["suggestion"] = new { type = "string" },
                        }
                    }
                }
            }
        });

        public string ModelKey(string? modelKey) =>
            modelKey ?? configuration["Review:Model"] ?? catalog.DefaultReviewer;

        public async Task<ReviewOutput> ReviewFileAsync(string fileName, string content, string? modelKey = null,
            ChangeView? change = null, CancellationToken cancellationToken = default)
        {
            var model = catalog.Get(ModelKey(modelKey));
            if (change is { IsEmpty: true })
            {
                // A change that leaves this file as it was has nothing to review
                return new ReviewOutput { FileName = fileName, Model = model.Key };
            }
            var request = BuildRequest(fileName, content, change);

            var stopwatch = Stopwatch.StartNew();
            var answer = await clients.Create(model).CompleteJsonAsync(request, cancellationToken);
            stopwatch.Stop();

            return new ReviewOutput
            {
                FileName = fileName,
                Model = model.Key,
                Findings = ParseFindings(answer, model.Key),
                InputTokens = answer.InputTokens,
                OutputTokens = answer.OutputTokens,
                Cost = model.CostOf(answer.InputTokens, answer.OutputTokens),
                Duration = stopwatch.Elapsed
            };
        }

        public ModelRequest BuildRequest(string fileName, string content, ChangeView? change = null)
        {
            var user = prompts.FormatUserPrompt(
                fileName, NumberLines(content, change), configurationService.Settings.AzureOpenAI.ContentLimit);
            var system = SystemPromptFor(prompts.GetSystemPrompt(fileName));
            if (change is not null)
            {
                system = $"{system}\n\n{ChangeInstructions}";
            }

            return new ModelRequest(system, user, "review_findings", Schema, MaxOutputTokens);
        }

        /// <summary>
        /// The whole system prompt sent to the model: the language's prompt followed by the output instructions
        /// </summary>
        public static string SystemPromptFor(string languagePrompt) => $"{languagePrompt.TrimEnd()}\n\n{OutputInstructions}";

        /// <summary>
        /// The file with its line numbers in a margin. For a change, a + beside each line it added or changed, and
        /// each removed line shown where it was, marked - and without a number
        /// </summary>
        public static string NumberLines(string content, ChangeView? change = null)
        {
            var lines = content.Replace("\r\n", "\n").Split('\n');
            var width = lines.Length.ToString().Length;
            var numbered = new StringBuilder();

            void AppendRemoved(int beforeLine)
            {
                if (change is not null && change.RemovedBefore.TryGetValue(beforeLine, out var removed))
                {
                    foreach (var line in removed)
                    {
                        numbered.Append(new string(' ', width)).Append(" - | ").Append(line).Append('\n');
                    }
                }
            }

            for (var i = 0; i < lines.Length; i++)
            {
                AppendRemoved(i + 1);
                numbered.Append((i + 1).ToString().PadLeft(width));
                if (change is not null)
                {
                    numbered.Append(change.Added.Contains(i + 1) ? " +" : "  ");
                }
                numbered.Append(" | ").Append(lines[i]).Append('\n');
            }
            AppendRemoved(lines.Length + 1);
            return numbered.ToString().TrimEnd('\n');
        }

        public static List<Finding> ParseFindings(ModelAnswer answer, string modelKey)
        {
            // Anthropic says max_tokens, chat completions APIs say length
            if (answer.StopReason?.Replace("_", "").ToLowerInvariant() is "maxtokens" or "length")
                throw new ModelAnswerException($"{modelKey} ran out of output tokens before finishing the review");

            try
            {
                return JsonSerializer.Deserialize<ReviewFindings>(answer.Json)?.Findings
                    ?? throw new ModelAnswerException($"{modelKey} returned no findings object");
            }
            catch (JsonException ex)
            {
                throw new ModelAnswerException($"{modelKey} returned JSON that does not match the schema", ex);
            }
        }
    }
}
