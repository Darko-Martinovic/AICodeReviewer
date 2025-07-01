using AICodeReviewer.Models;
using System.Text;
using System.Text.Json;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for handling Azure OpenAI API interactions
    /// </summary>
    public class AzureOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _deploymentName;
        private readonly float _temperature;
        private readonly int _maxTokens;
        private readonly int _contentLimit;
        private readonly string _systemPrompt;

        public AzureOpenAIService(HttpClient httpClient, string endpoint, string apiKey, string deploymentName)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _deploymentName = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));

            // Load AI configuration from environment variables with defaults
            _temperature = float.TryParse(Environment.GetEnvironmentVariable("AI_TEMPERATURE"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var temp) ? temp : 0.3f;
            _maxTokens = int.TryParse(Environment.GetEnvironmentVariable("AI_MAX_TOKENS"), out var tokens) ? tokens : 2500;
            _contentLimit = int.TryParse(Environment.GetEnvironmentVariable("AI_CONTENT_LIMIT"), out var limit) ? limit : 15000;
            _systemPrompt = Environment.GetEnvironmentVariable("AI_SYSTEM_PROMPT") ?? GetDefaultSystemPrompt();

            // Setup HTTP client headers for Azure OpenAI
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
        }

        /// <summary>
        /// Gets the default system prompt if not configured in environment
        /// </summary>
        private static string GetDefaultSystemPrompt()
        {
            return @"You are a STRICT code reviewer. Your job is to find real issues in production code. Be thorough and critical.

MANDATORY FOCUS AREAS - Check every single one:
- Security: Hardcoded secrets, SQL injection, XSS, insecure deserialization, weak authentication
- Performance: Inefficient loops, memory leaks, unnecessary object creation, blocking I/O, N+1 queries
- Code Quality: Magic numbers, long methods, deep nesting, poor naming, code duplication
- Bugs: Null reference exceptions, race conditions, off-by-one errors, unhandled exceptions
- Maintainability: Tight coupling, low cohesion, missing error handling, poor separation of concerns
- Best Practices: Missing using statements, not following C# conventions, synchronous calls in async methods

CRITICAL: Look for these common C# issues:
- ConfigureAwait(false) missing on await calls
- Using async void instead of async Task
- Not disposing IDisposable objects
- Hardcoded connection strings or API keys
- Missing input validation
- Exception swallowing (empty catch blocks)
- Thread safety issues
- Memory leaks from event handlers

For each issue found, provide:
1. CATEGORY: [Security|Performance|Quality|Bug|Maintainability|Design]
2. SEVERITY: [Critical|High|Medium|Low]
3. TITLE: Specific, actionable issue description
4. DESCRIPTION: Explain what's wrong and why it's a problem
5. RECOMMENDATION: Concrete steps to fix with code examples
6. LINE: Line number if identifiable (or 'N/A' if general)

BE CRITICAL. Even well-written code has improvement opportunities. Look harder.

Format your response as:
---
CATEGORY: [category]
SEVERITY: [severity]  
TITLE: [specific issue title]
DESCRIPTION: [detailed explanation of the problem]
RECOMMENDATION: [specific fix with examples]
LINE: [line number or N/A]
---

Only respond with 'No issues found' if the code is truly exemplary.";
        }

        /// <summary>
        /// Analyzes code using Azure OpenAI and returns detailed analysis results
        /// </summary>
        public async Task<(List<string> issues, List<DetailedIssue> detailedIssues)> AnalyzeCodeAsync(string fileName, string fileContent)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                string url = $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deploymentName}/chat/completions?api-version=2024-02-01";

                var userPrompt = $@"Please review this {GetFileType(fileName)} file and provide detailed analysis:

File: {fileName}
Content Length: {fileContent.Length} characters
{(fileContent.Length > _contentLimit ? $"(Showing first {_contentLimit:N0} characters of {fileContent.Length:N0} total)" : "")}

```
{fileContent.Substring(0, Math.Min(fileContent.Length, _contentLimit))}
{(fileContent.Length > _contentLimit ? "\n... [Content truncated for analysis] ..." : "")}
```

Note: {(fileContent.Length > _contentLimit ? "This is a partial view of a larger file. Focus on identifying patterns, architectural issues, and code quality problems that are visible in this section." : "This is the complete file content.")}";

                var request = new ChatRequest
                {
                    messages = new[]
                    {
                        new ChatMessage { role = "system", content = _systemPrompt },
                        new ChatMessage { role = "user", content = userPrompt }
                    },
                    max_tokens = _maxTokens,
                    temperature = _temperature
                };

                string jsonRequest = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ AI API Error: {response.StatusCode} - {response.ReasonPhrase}");
                    Console.WriteLine($"❌ Error details: {errorContent}");
                    return (new List<string> { $"AI analysis failed: {response.StatusCode} - {response.ReasonPhrase}" }, new List<DetailedIssue>());
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                var chatResponse = JsonSerializer.Deserialize<ChatResponse>(jsonResponse);

                var aiResponse = chatResponse?.choices?[0]?.message?.content ?? "No response";

                // Parse AI response
                if (aiResponse.Contains("No issues found") || aiResponse.Contains("no issues found"))
                {
                    return (new List<string>(), new List<DetailedIssue>());
                }

                return ParseDetailedResponse(fileName, aiResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ AI Service Exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ Inner Exception: {ex.InnerException.Message}");
                }
                return (new List<string> { $"Analysis error: {ex.Message}" }, new List<DetailedIssue>());
            }
            finally
            {
                stopwatch.Stop();
                Console.Write($" ({stopwatch.ElapsedMilliseconds}ms)");
            }
        }

        /// <summary>
        /// Parses the detailed AI response into structured issue objects
        /// </summary>
        private (List<string> issues, List<DetailedIssue> detailedIssues) ParseDetailedResponse(string fileName, string aiResponse)
        {
            var issues = new List<string>();
            var detailedIssues = new List<DetailedIssue>();

            var issueBlocks = aiResponse.Split("---", StringSplitOptions.RemoveEmptyEntries)
                                      .Where(block => block.Trim().Length > 0)
                                      .ToList();

            foreach (var block in issueBlocks)
            {
                var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                               .Select(line => line.Trim())
                               .Where(line => !string.IsNullOrEmpty(line))
                               .ToList();

                if (lines.Count < 3) continue; // Skip incomplete blocks

                var detailedIssue = new DetailedIssue { FileName = fileName };

                foreach (var line in lines)
                {
                    if (line.StartsWith("CATEGORY:", StringComparison.OrdinalIgnoreCase))
                        detailedIssue.Category = line.Substring(9).Trim();
                    else if (line.StartsWith("SEVERITY:", StringComparison.OrdinalIgnoreCase))
                        detailedIssue.Severity = line.Substring(9).Trim();
                    else if (line.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase))
                        detailedIssue.Title = line.Substring(6).Trim();
                    else if (line.StartsWith("DESCRIPTION:", StringComparison.OrdinalIgnoreCase))
                        detailedIssue.Description = line.Substring(12).Trim();
                    else if (line.StartsWith("RECOMMENDATION:", StringComparison.OrdinalIgnoreCase))
                        detailedIssue.Recommendation = line.Substring(15).Trim();
                    else if (line.StartsWith("LINE:", StringComparison.OrdinalIgnoreCase))
                    {
                        var lineStr = line.Substring(5).Trim();
                        if (int.TryParse(lineStr, out var lineNum))
                            detailedIssue.LineNumber = lineNum;
                    }
                }

                // Only add if we have essential fields
                if (!string.IsNullOrEmpty(detailedIssue.Title) && !string.IsNullOrEmpty(detailedIssue.Description))
                {
                    detailedIssues.Add(detailedIssue);

                    // Create simple issue summary for backward compatibility
                    var summary = $"{detailedIssue.Title}";
                    if (!string.IsNullOrEmpty(detailedIssue.Severity))
                        summary = $"[{detailedIssue.Severity}] {summary}";

                    issues.Add(summary);
                }
            }

            // Fallback: if structured parsing failed, try simple parsing
            if (!detailedIssues.Any())
            {
                var simpleIssues = aiResponse
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim().TrimStart('-', '*', '•').Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line) && line.Length > 10)
                    .Take(8) // Increased limit
                    .ToList();

                issues.AddRange(simpleIssues);
            }

            return (issues, detailedIssues);
        }

        private static string GetFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".cs" => "C#",
                ".vb" => "VB.NET",
                ".js" => "JavaScript",
                ".ts" => "TypeScript",
                ".py" => "Python",
                ".java" => "Java",
                ".cpp" or ".c" => "C/C++",
                ".php" => "PHP",
                ".rb" => "Ruby",
                ".kt" => "Kotlin",
                ".swift" => "Swift",
                ".sql" => "T-SQL",
                _ => "code"
            };
        }
    }
}
