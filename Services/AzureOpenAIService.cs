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

        public AzureOpenAIService(HttpClient httpClient, string endpoint, string apiKey, string deploymentName)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _deploymentName = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));

            // Setup HTTP client headers for Azure OpenAI
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
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

                var systemPrompt = @"You are an expert code reviewer. Analyze the provided code and identify potential issues, bugs, or improvements. 

Focus on:
- Security vulnerabilities
- Performance issues  
- Code quality and best practices
- Potential bugs
- Maintainability concerns
- Design patterns and architecture

For each issue found, provide:
1. CATEGORY: [Security|Performance|Quality|Bug|Maintainability|Design]
2. SEVERITY: [Critical|High|Medium|Low]
3. TITLE: Brief description of the issue
4. DESCRIPTION: Detailed explanation of what's wrong
5. RECOMMENDATION: Specific steps to fix the issue
6. LINE: Line number if applicable (or 'N/A')

Format your response as:
---
CATEGORY: [category]
SEVERITY: [severity]  
TITLE: [title]
DESCRIPTION: [detailed description]
RECOMMENDATION: [how to fix]
LINE: [line number or N/A]
---

If no significant issues are found, respond with 'No issues found'.";

                var userPrompt = $@"Please review this {GetFileType(fileName)} file and provide detailed analysis:

File: {fileName}
```
{fileContent.Substring(0, Math.Min(fileContent.Length, 5000))}
```";

                var request = new ChatRequest
                {
                    messages = new[]
                    {
                        new ChatMessage { role = "system", content = systemPrompt },
                        new ChatMessage { role = "user", content = userPrompt }
                    },
                    max_tokens = 1500,  // Increased for detailed responses
                    temperature = 0.3f
                };

                string jsonRequest = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    return (new List<string> { "AI analysis failed" }, new List<DetailedIssue>());
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
