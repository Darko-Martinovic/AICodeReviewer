using AICodeReviewer.Models;
using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for handling Azure OpenAI API interactions
    /// </summary>
    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _deploymentName;
        private readonly AzureOpenAISettings _settings;
        private readonly IPromptManagementService _promptManagementService;

        public AzureOpenAIService(
            HttpClient httpClient,
            string endpoint,
            string apiKey,
            string deploymentName,
            IConfigurationService configurationService,
            IPromptManagementService promptManagementService
        )
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _deploymentName =
                deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));

            var configService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _settings = configService.Settings.AzureOpenAI;
            _promptManagementService = promptManagementService ?? throw new ArgumentNullException(nameof(promptManagementService));

            // Ensure we have a system prompt
            if (string.IsNullOrEmpty(_settings.SystemPrompt))
            {
                _settings.SystemPrompt = GetDefaultSystemPrompt();
            }

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
        /// Analyzes code using Azure OpenAI and returns detailed analysis results with token usage
        /// </summary>
        public async Task<(
            List<string> issues,
            List<DetailedIssue> detailedIssues,
            Usage usage
        )> AnalyzeCodeAsync(string fileName, string fileContent)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                string url =
                    $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deploymentName}/chat/completions?api-version={_settings.ApiVersion}";

                // Get language-specific prompts
                var systemPrompt = _promptManagementService.GetSystemPrompt(fileName);

                // ENHANCED: Add additional strictness to force critical analysis
                systemPrompt += "\n\n🚨 CRITICAL ANALYSIS REQUIREMENT:\n" +
                    "You MUST find AT LEAST 2-3 specific improvement opportunities in this code. " +
                    "Even if the code appears well-written, look for:\n" +
                    "- Missing error handling, validation, or edge cases\n" +
                    "- Performance optimizations (async patterns, caching, efficiency)\n" +
                    "- Security improvements (input validation, data exposure)\n" +
                    "- Code quality issues (naming, complexity, maintainability)\n" +
                    "- Modern language features not being used\n" +
                    "- Documentation or testing gaps\n" +
                    "- Architecture or design pattern improvements\n\n" +
                    "DO NOT respond with 'No issues found' unless you have thoroughly examined ALL " +
                    "aspects and can provide detailed justification for why the code is absolutely perfect.";

                var userPrompt = _promptManagementService.FormatUserPrompt(fileName, fileContent, _settings.ContentLimit);

                // Log which prompt type is being used
                var hasLanguageSpecificPrompts = _promptManagementService.HasLanguageSpecificPrompts(fileName);
                var language = _promptManagementService.GetType().Assembly.GetName().Name; // This will be the service name
                Console.Write($"    🤖 Using {(hasLanguageSpecificPrompts ? "language-specific" : "default")} prompts for {fileName}...");

                // Enhanced debugging - log the actual prompts being used
                Console.WriteLine($"\n🔍 DEBUG - System Prompt Length: {systemPrompt.Length} characters");
                Console.WriteLine($"🔍 DEBUG - System Prompt Preview: {systemPrompt.Substring(0, Math.Min(200, systemPrompt.Length))}...");
                Console.WriteLine($"🔍 DEBUG - User Prompt Length: {userPrompt.Length} characters");
                Console.WriteLine($"🔍 DEBUG - User Prompt Preview: {userPrompt.Substring(0, Math.Min(200, userPrompt.Length))}...");

                var request = new ChatRequest
                {
                    messages = new[]
                    {
                        new ChatMessage { role = "system", content = systemPrompt },
                        new ChatMessage { role = "user", content = userPrompt }
                    },
                    max_tokens = _settings.MaxTokens,
                    temperature = (float)Math.Max(_settings.Temperature, 0.8) // Higher temperature for more critical analysis
                };

                string jsonRequest = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(
                        $"❌ AI API Error: {response.StatusCode} - {response.ReasonPhrase}"
                    );
                    Console.WriteLine($"❌ Error details: {errorContent}");
                    return (
                        new List<string>
                        {
                            $"AI analysis failed: {response.StatusCode} - {response.ReasonPhrase}"
                        },
                        new List<DetailedIssue>(),
                        new Usage()
                    );
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                var chatResponse = JsonSerializer.Deserialize<ChatResponse>(jsonResponse);

                var aiResponse = chatResponse?.choices?[0]?.message?.content ?? "No response";
                var usage = chatResponse?.usage ?? new Usage();

                // Add debugging to see what the AI actually returned
                Console.WriteLine($"\n🔍 DEBUG - AI Response for {fileName}:");
                Console.WriteLine($"📝 Response length: {aiResponse.Length} characters");
                Console.WriteLine($"📝 First 200 chars: {aiResponse.Substring(0, Math.Min(200, aiResponse.Length))}...");

                // REMOVED: Don't filter out responses - always try to parse them
                // The AI should be finding issues if the prompts are strong enough
                Console.WriteLine($"🔍 DEBUG - Proceeding to parse detailed response");
                var (issues, detailedIssues) = ParseDetailedResponse(fileName, aiResponse);
                Console.WriteLine($"🔍 DEBUG - Parsed: {issues.Count} issues, {detailedIssues.Count} detailed issues");
                return (issues, detailedIssues, usage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ AI Service Exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ Inner Exception: {ex.InnerException.Message}");
                }
                return (
                    new List<string> { $"Analysis error: {ex.Message}" },
                    new List<DetailedIssue>(),
                    new Usage()
                );
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
        private (List<string> issues, List<DetailedIssue> detailedIssues) ParseDetailedResponse(
            string fileName,
            string aiResponse
        )
        {
            var issues = new List<string>();
            var detailedIssues = new List<DetailedIssue>();

            var issueBlocks = aiResponse
                .Split("---", StringSplitOptions.RemoveEmptyEntries)
                .Where(block => block.Trim().Length > 0)
                .ToList();

            foreach (var block in issueBlocks)
            {
                var lines = block
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line))
                    .ToList();

                if (lines.Count < 3)
                    continue; // Skip incomplete blocks

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
                if (
                    !string.IsNullOrEmpty(detailedIssue.Title)
                    && !string.IsNullOrEmpty(detailedIssue.Description)
                )
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
                Console.WriteLine($"🔍 DEBUG - Structured parsing failed, using fallback for response: '{aiResponse}'");

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


    }
}
