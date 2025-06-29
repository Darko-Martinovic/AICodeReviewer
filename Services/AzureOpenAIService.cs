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
        /// Analyzes code using Azure OpenAI and returns a list of issues found
        /// </summary>
        public async Task<List<string>> AnalyzeCodeAsync(string fileName, string fileContent)
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

Provide a concise list of specific issues found. If no significant issues are found, respond with 'No issues found'.
Keep each issue to one line and be specific about the problem.";

                var userPrompt = $@"Please review this {GetFileType(fileName)} file:

File: {fileName}
```
{fileContent.Substring(0, Math.Min(fileContent.Length, 3000))}
```";

                var request = new ChatRequest
                {
                    messages = new[]
                    {
                        new ChatMessage { role = "system", content = systemPrompt },
                        new ChatMessage { role = "user", content = userPrompt }
                    },
                    max_tokens = 800,
                    temperature = 0.3f
                };

                string jsonRequest = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<string> { "AI analysis failed" };
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                var chatResponse = JsonSerializer.Deserialize<ChatResponse>(jsonResponse);

                var aiResponse = chatResponse?.choices?[0]?.message?.content ?? "No response";

                // Parse AI response into individual issues
                if (aiResponse.Contains("No issues found") || aiResponse.Contains("no issues found"))
                {
                    return new List<string>();
                }

                // Split response into lines and clean up
                var issues = aiResponse
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim().TrimStart('-', '*', '•').Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line) && line.Length > 10)
                    .Take(5) // Limit to top 5 issues
                    .ToList();

                return issues;
            }
            catch (Exception ex)
            {
                return new List<string> { $"Analysis error: {ex.Message}" };
            }
            finally
            {
                stopwatch.Stop();
                Console.Write($" ({stopwatch.ElapsedMilliseconds}ms)");
            }
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
