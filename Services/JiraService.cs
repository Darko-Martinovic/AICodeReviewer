using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for handling Jira integration and ticket operations
    /// </summary>
    public class JiraService : IDisposable
    {
        private readonly string? _jiraBaseUrl;
        private readonly string? _jiraApiToken;
        private readonly string? _jiraUserEmail;
        private readonly HttpClient _httpClient;

        public JiraService()
        {
            // Load Jira configuration from environment variables
            _jiraBaseUrl = Environment.GetEnvironmentVariable("JIRA_BASE_URL");
            _jiraApiToken = Environment.GetEnvironmentVariable("JIRA_API_TOKEN");
            _jiraUserEmail = Environment.GetEnvironmentVariable("JIRA_USER_EMAIL");

            // Debug: Check what environment variables are loaded
            Console.WriteLine($"🔍 Debug: JIRA_BASE_URL = {(_jiraBaseUrl != null ? "SET" : "NOT SET")}");
            Console.WriteLine($"🔍 Debug: JIRA_API_TOKEN = {(_jiraApiToken != null ? "SET" : "NOT SET")}");
            Console.WriteLine($"🔍 Debug: JIRA_USER_EMAIL = {(_jiraUserEmail != null ? "SET" : "NOT SET")}");

            _httpClient = new HttpClient();

            // Configure HttpClient for Jira API if credentials are available
            if (IsJiraConfigured())
            {
                var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_jiraUserEmail}:{_jiraApiToken}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                Console.WriteLine($"🔍 Debug: JIRA API configured successfully");
            }
            else
            {
                Console.WriteLine($"🔍 Debug: JIRA API not configured - missing environment variables");
            }
        }

        /// <summary>
        /// Extracts Jira ticket keys from a pull request title
        /// Supports patterns like OPS-123, PROJ-456, etc.
        /// </summary>
        public List<string> ExtractTicketKeysFromTitle(string prTitle)
        {
            if (string.IsNullOrWhiteSpace(prTitle))
                return new List<string>();

            // Pattern to match Jira ticket keys (e.g., OPS-123, PROJ-456)
            // Updated to handle cases like "#OPS-8", "OPS-8", "OPS-123: description", etc.
            var pattern = @"(?:^|[^A-Z0-9])([A-Z]{2,10}-\d+)(?=[^A-Z0-9]|$)";
            var matches = Regex.Matches(prTitle, pattern, RegexOptions.IgnoreCase);

            var tickets = matches.Cast<Match>()
                         .Select(m => m.Groups[1].Value.ToUpper()) // Use Groups[1] to get the captured group
                         .Distinct()
                         .ToList();

            // Debug output to show what was extracted
            Console.WriteLine($"🔍 Debug: Extracting tickets from PR title: \"{prTitle}\"");
            Console.WriteLine($"🔍 Debug: Found {tickets.Count} ticket(s): {string.Join(", ", tickets)}");

            return tickets;
        }

        /// <summary>
        /// Updates Jira tickets with code review results (simulated)
        /// </summary>
        public async Task UpdateTicketsWithReviewResultsAsync(
            List<string> ticketKeys,
            string prNumber,
            string author,
            int issueCount,
            List<string> reviewedFiles,
            List<string> topIssues)
        {
            if (!ticketKeys.Any())
            {
                Console.WriteLine("🎫 No Jira tickets found in PR title");
                Console.WriteLine("💡 Tip: Include JIRA ticket keys in PR title (e.g., 'OPS-123: Add new feature')");
                return;
            }

            await Task.Delay(100); // Simulate API call

            Console.WriteLine($"🎫 Processing {ticketKeys.Count} JIRA ticket(s): {string.Join(", ", ticketKeys)}");

            foreach (var ticketKey in ticketKeys)
            {
                Console.WriteLine($"\n📋 Updating ticket: {ticketKey}");

                if (IsJiraConfigured())
                {
                    Console.WriteLine("   ✅ Connected to Jira API");
                    await UpdateJiraTicketAsync(ticketKey, prNumber, author, issueCount, reviewedFiles, topIssues);
                }
                else
                {
                    Console.WriteLine("   ⚠️  Jira not configured - showing simulated update:");
                    Console.WriteLine("   💡 Set JIRA_URL, JIRA_API_TOKEN, and JIRA_EMAIL environment variables");
                    await SimulateJiraUpdateAsync(ticketKey, prNumber, author, issueCount, reviewedFiles, topIssues);
                }
            }

            Console.WriteLine($"\n✅ JIRA ticket update process completed for {ticketKeys.Count} ticket(s): {string.Join(", ", ticketKeys)}");
        }

        /// <summary>
        /// Checks if Jira is properly configured
        /// </summary>
        private bool IsJiraConfigured()
        {
            return !string.IsNullOrWhiteSpace(_jiraBaseUrl) &&
                   !string.IsNullOrWhiteSpace(_jiraApiToken) &&
                   !string.IsNullOrWhiteSpace(_jiraUserEmail);
        }

        /// <summary>
        /// Updates a Jira ticket with code review information (actual implementation)
        /// </summary>
        private async Task UpdateJiraTicketAsync(
            string ticketKey,
            string prNumber,
            string author,
            int issueCount,
            List<string> reviewedFiles,
            List<string> topIssues)
        {
            try
            {
                string severity = GetIssueSeverity(issueCount);

                // Create comment content
                var comment = CreateJiraComment(prNumber, author, issueCount, reviewedFiles, topIssues);

                // Create comment payload for Jira API using the working format
                var commentPayload = new
                {
                    update = new
                    {
                        comment = new[]
                        {
                            new
                            {
                                add = new
                                {
                                    body = new
                                    {
                                        type = "doc",
                                        version = 1,
                                        content = new[]
                                        {
                                            new
                                            {
                                                type = "paragraph",
                                                content = new[]
                                                {
                                                    new
                                                    {
                                                        type = "text",
                                                        text = comment
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(commentPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Make API call to update issue (PUT method)
                var url = $"{_jiraBaseUrl}/rest/api/3/issue/{ticketKey}";

                // Maximum debug output
                Console.WriteLine($"   🔍 Debug: Making PUT request to {url}");
                Console.WriteLine($"   🔍 Debug: HTTP Method: PUT");
                Console.WriteLine($"   🔍 Debug: Request Headers:");
                foreach (var header in _httpClient.DefaultRequestHeaders)
                {
                    Console.WriteLine($"     {header.Key}: {string.Join(", ", header.Value)}");
                }
                Console.WriteLine($"   🔍 Debug: Content Headers:");
                foreach (var header in content.Headers)
                {
                    Console.WriteLine($"     {header.Key}: {string.Join(", ", header.Value)}");
                }
                Console.WriteLine($"   🔍 Debug: Payload: {json}");

                var response = await _httpClient.PutAsync(url, content);

                Console.WriteLine($"   🔍 Debug: Response Status Code: {response.StatusCode}");
                Console.WriteLine($"   🔍 Debug: Response Headers:");
                foreach (var header in response.Headers)
                {
                    Console.WriteLine($"     {header.Key}: {string.Join(", ", header.Value)}");
                }
                foreach (var header in response.Content.Headers)
                {
                    Console.WriteLine($"     {header.Key}: {string.Join(", ", header.Value)}");
                }
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   🔍 Debug: Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"   ✅ Successfully added comment to {ticketKey}");
                    Console.WriteLine($"   🔗 Linked PR #{prNumber}");
                    Console.WriteLine($"   📊 Review status: {issueCount} issues ({severity})");
                    Console.WriteLine($"   🎯 Recommendation: {GetRecommendation(issueCount)}");
                }
                else
                {
                    Console.WriteLine($"   ❌ Failed to update {ticketKey}: {response.StatusCode}");
                    Console.WriteLine($"   📝 Error details: {responseBody}");
                    Console.WriteLine($"   🔄 Falling back to simulated update");
                    await SimulateJiraUpdateAsync(ticketKey, prNumber, author, issueCount, reviewedFiles, topIssues);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error updating {ticketKey}: {ex.Message}");
                Console.WriteLine($"   ❌ Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"   📝 Falling back to simulated update");
                await SimulateJiraUpdateAsync(ticketKey, prNumber, author, issueCount, reviewedFiles, topIssues);
            }
        }

        /// <summary>
        /// Simulates a Jira ticket update for demonstration purposes
        /// </summary>
        private async Task SimulateJiraUpdateAsync(
            string ticketKey,
            string prNumber,
            string author,
            int issueCount,
            List<string> reviewedFiles,
            List<string> topIssues)
        {
            await Task.Delay(100);

            string severity = GetIssueSeverity(issueCount);

            Console.WriteLine($"   📝 Comment added to {ticketKey}:");
            Console.WriteLine($"      \"🤖 AI Code Review completed for PR #{prNumber}\"");
            Console.WriteLine($"      \"👤 Author: {author}\"");
            Console.WriteLine($"      \"📁 Files reviewed: {reviewedFiles.Count}\"");
            Console.WriteLine($"      \"🔍 Issues found: {issueCount} ({severity} severity)\"");

            if (topIssues.Any())
            {
                Console.WriteLine($"      \"🔍 Top issues:\"");
                foreach (var issue in topIssues.Take(2))
                {
                    Console.WriteLine($"      \"  • {issue}\"");
                }
            }

            // Simulate workflow actions based on issue count
            if (issueCount == 0)
            {
                Console.WriteLine($"   ✅ Status: Ready for merge");
                Console.WriteLine($"   🎯 Recommendation: Approve and merge");
            }
            else if (issueCount <= 2)
            {
                Console.WriteLine($"   ⚠️  Status: Review recommended");
                Console.WriteLine($"   🎯 Recommendation: Review minor issues before merge");
            }
            else
            {
                Console.WriteLine($"   🚫 Status: Fixes required before merge");
                Console.WriteLine($"   🎯 Recommendation: Address issues before approval");
            }
        }

        /// <summary>
        /// Gets a human-readable severity description based on issue count
        /// </summary>
        private static string GetIssueSeverity(int issueCount)
        {
            return issueCount switch
            {
                0 => "Clean",
                <= 2 => "Low",
                <= 5 => "Medium",
                _ => "High"
            };
        }

        /// <summary>
        /// Gets a recommendation based on issue count
        /// </summary>
        private static string GetRecommendation(int issueCount)
        {
            return issueCount switch
            {
                0 => "Approve and merge",
                <= 2 => "Review minor issues before merge",
                <= 5 => "Address issues before approval",
                _ => "Fix critical issues before merge"
            };
        }

        /// <summary>
        /// Creates a formatted comment for Jira tickets
        /// </summary>
        public string CreateJiraComment(
            string prNumber,
            string author,
            int issueCount,
            List<string> reviewedFiles,
            List<string> topIssues)
        {
            var severity = GetIssueSeverity(issueCount);
            var comment = $"🤖 *AI Code Review Completed*\n\n";
            comment += $"*Pull Request:* #{prNumber}\n";
            comment += $"*Author:* {author}\n";
            comment += $"*Files Reviewed:* {reviewedFiles.Count}\n";
            comment += $"*Issues Found:* {issueCount} ({severity} severity)\n\n";

            if (topIssues.Any())
            {
                comment += "*Top Issues:*\n";
                foreach (var issue in topIssues.Take(3))
                {
                    comment += $"• {issue}\n";
                }

                if (topIssues.Count > 3)
                {
                    comment += $"... and {topIssues.Count - 3} more issue(s)\n";
                }
            }

            return comment;
        }

        /// <summary>
        /// Disposes of the HttpClient resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
