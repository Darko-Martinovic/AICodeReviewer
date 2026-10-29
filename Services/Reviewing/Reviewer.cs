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
        Task<ReviewOutput> ReviewFileAsync(string fileName, string content, string? modelKey = null, CancellationToken cancellationToken = default);
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

        public async Task<ReviewOutput> ReviewFileAsync(string fileName, string content, string? modelKey = null, CancellationToken cancellationToken = default)
        {
            var model = catalog.Get(ModelKey(modelKey));
            var request = BuildRequest(fileName, content);

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

        public ModelRequest BuildRequest(string fileName, string content)
        {
            var user = prompts.FormatUserPrompt(
                fileName, NumberLines(content), configurationService.Settings.AzureOpenAI.ContentLimit);

            return new ModelRequest(SystemPromptFor(prompts.GetSystemPrompt(fileName)), user, "review_findings", Schema, MaxOutputTokens);
        }

        /// <summary>
        /// The whole system prompt sent to the model: the language's prompt followed by the output instructions
        /// </summary>
        public static string SystemPromptFor(string languagePrompt) => $"{languagePrompt.TrimEnd()}\n\n{OutputInstructions}";

        public static string NumberLines(string content)
        {
            var lines = content.Replace("\r\n", "\n").Split('\n');
            var width = lines.Length.ToString().Length;
            var numbered = new StringBuilder();
            for (var i = 0; i < lines.Length; i++)
            {
                numbered.Append((i + 1).ToString().PadLeft(width)).Append(" | ").Append(lines[i]).Append('\n');
            }
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
