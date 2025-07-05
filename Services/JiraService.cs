using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using AICodeReviewer.Models;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for handling Jira integration and ticket operations
    /// </summary>
    public class JiraService : IJiraService, IDisposable
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
            Console.WriteLine(
                $"🔍 Debug: JIRA_BASE_URL = {(_jiraBaseUrl != null ? "SET" : "NOT SET")}"
            );
            Console.WriteLine(
                $"🔍 Debug: JIRA_API_TOKEN = {(_jiraApiToken != null ? "SET" : "NOT SET")}"
            );
            Console.WriteLine(
                $"🔍 Debug: JIRA_USER_EMAIL = {(_jiraUserEmail != null ? "SET" : "NOT SET")}"
            );

            _httpClient = new HttpClient();

            // Configure HttpClient for Jira API if credentials are available
            if (IsJiraConfigured())
            {
                var authString = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{_jiraUserEmail}:{_jiraApiToken}")
                );
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
                );
                Console.WriteLine($"🔍 Debug: JIRA API configured successfully");
            }
            else
            {
                Console.WriteLine(
                    $"🔍 Debug: JIRA API not configured - missing environment variables"
                );
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

            var tickets = matches
                .Cast<Match>()
                .Select(m => m.Groups[1].Value.ToUpper()) // Use Groups[1] to get the captured group
                .Distinct()
                .ToList();

            // Debug output to show what was extracted
            Console.WriteLine($"🔍 Debug: Extracting tickets from PR title: \"{prTitle}\"");
            Console.WriteLine(
                $"🔍 Debug: Found {tickets.Count} ticket(s): {string.Join(", ", tickets)}"
            );

            return tickets;
        }

        /// <summary>
        /// Updates Jira tickets with detailed code review results
        /// </summary>
        public async Task UpdateTicketsWithReviewResultsAsync(
            List<string> ticketKeys,
            string prNumber,
            string author,
            int issueCount,
            List<string> reviewedFiles,
            List<string> topIssues,
            List<DetailedIssue>? detailedIssues = null
        )
        {
            if (!ticketKeys.Any())
            {
                Console.WriteLine("🎫 No Jira tickets found in PR title");
                Console.WriteLine(
                    "💡 Tip: Include JIRA ticket keys in PR title (e.g., 'OPS-123: Add new feature')"
                );
                return;
            }

            await Task.Delay(100); // Simulate API call

            Console.WriteLine(
                $"🎫 Processing {ticketKeys.Count} JIRA ticket(s): {string.Join(", ", ticketKeys)}"
            );

            foreach (var ticketKey in ticketKeys)
            {
                Console.WriteLine($"\n📋 Updating ticket: {ticketKey}");

                if (IsJiraConfigured())
                {
                    Console.WriteLine("   ✅ Connected to Jira API");
                    await UpdateJiraTicketAsync(
                        ticketKey,
                        prNumber,
                        author,
                        issueCount,
                        reviewedFiles,
                        topIssues,
                        detailedIssues
                    );
                }
                else
                {
                    Console.WriteLine("   ⚠️  Jira not configured - showing simulated update:");
                    Console.WriteLine(
                        "   💡 Set JIRA_URL, JIRA_API_TOKEN, and JIRA_EMAIL environment variables"
                    );
                    await SimulateJiraUpdateAsync(
                        ticketKey,
                        prNumber,
                        author,
                        issueCount,
                        reviewedFiles,
                        topIssues
                    );
                }
            }

            Console.WriteLine(
                $"\n✅ JIRA ticket update process completed for {ticketKeys.Count} ticket(s): {string.Join(", ", ticketKeys)}"
            );
        }

        /// <summary>
        /// Adds labels to a JIRA ticket
        /// </summary>
        private async Task AddLabelsToTicketAsync(string ticketKey, List<string> labels)
        {
            try
            {
                var labelPayload = new
                {
                    update = new
                    {
                        labels = labels.Select(label => new { add = label }).ToArray<object>()
                    }
                };

                var json = JsonSerializer.Serialize(labelPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_jiraBaseUrl}/rest/api/3/issue/{ticketKey}";
                var response = await _httpClient.PutAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine(
                        $"   🏷️  Added labels to {ticketKey}: {string.Join(", ", labels)}"
                    );
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(
                        $"   ⚠️  Failed to add labels to {ticketKey}: {response.StatusCode}"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  Error adding labels to {ticketKey}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if Jira is properly configured
        /// </summary>
        private bool IsJiraConfigured()
        {
            return !string.IsNullOrWhiteSpace(_jiraBaseUrl)
                && !string.IsNullOrWhiteSpace(_jiraApiToken)
                && !string.IsNullOrWhiteSpace(_jiraUserEmail);
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
            List<string> topIssues,
            List<DetailedIssue>? detailedIssues = null
        )
        {
            try
            {
                string severity = GetIssueSeverity(issueCount);

                // Create comment content using ADF format
                var commentBody = CreateJiraCommentAdf(
                    prNumber,
                    author,
                    issueCount,
                    reviewedFiles,
                    topIssues,
                    detailedIssues
                );

                // Create comment payload for Jira API using the working format
                var commentPayload = new
                {
                    update = new { comment = new[] { new { add = new { body = commentBody } } } }
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

                    // Add labels based on review results
                    var labels = new List<string> { "ai-code-review", $"pr-{prNumber}" };
                    labels.Add($"severity-{severity.ToLower()}");

                    if (issueCount == 0)
                        labels.Add("ready-for-merge");
                    else if (issueCount <= 2)
                        labels.Add("minor-issues");
                    else if (issueCount <= 5)
                        labels.Add("review-required");
                    else
                        labels.Add("critical-issues");

                    await AddLabelsToTicketAsync(ticketKey, labels);
                }
                else
                {
                    Console.WriteLine($"   ❌ Failed to update {ticketKey}: {response.StatusCode}");
                    Console.WriteLine($"   📝 Error details: {responseBody}");
                    Console.WriteLine($"   🔄 Falling back to simulated update");
                    await SimulateJiraUpdateAsync(
                        ticketKey,
                        prNumber,
                        author,
                        issueCount,
                        reviewedFiles,
                        topIssues
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error updating {ticketKey}: {ex.Message}");
                Console.WriteLine($"   ❌ Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"   📝 Falling back to simulated update");
                await SimulateJiraUpdateAsync(
                    ticketKey,
                    prNumber,
                    author,
                    issueCount,
                    reviewedFiles,
                    topIssues
                );
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
            List<string> topIssues
        )
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
        /// Gets the status color for Jira panels based on issue count
        /// </summary>
        private static string GetStatusColor(int issueCount)
        {
            return issueCount switch
            {
                0 => "success",
                <= 2 => "note",
                <= 5 => "warning",
                _ => "error"
            };
        }

        /// <summary>
        /// Gets the status text based on issue count
        /// </summary>
        private static string GetStatusText(int issueCount)
        {
            return issueCount switch
            {
                0 => "✅ Ready for Merge",
                <= 2 => "⚠️ Minor Issues Found",
                <= 5 => "🔶 Review Required",
                _ => "🚨 Critical Issues"
            };
        }

        /// <summary>
        /// Gets the severity color for text formatting
        /// </summary>
        private static string GetSeverityColor(int issueCount)
        {
            return issueCount switch
            {
                0 => "#006644", // Green for clean
                <= 2 => "#FFA500", // Orange for low
                <= 5 => "#FF4500", // Red-orange for medium
                _ => "#DC143C" // Dark red for high
            };
        }

        /// <summary>
        /// Creates a beautifully formatted comment for Jira tickets using Atlassian Document Format (ADF)
        /// </summary>
        public object CreateJiraCommentAdf(
            string prNumber,
            string author,
            int issueCount,
            List<string> reviewedFiles,
            List<string> topIssues,
            List<DetailedIssue>? detailedIssues = null
        )
        {
            var severity = GetIssueSeverity(issueCount);
            var recommendation = GetRecommendation(issueCount);
            var statusColor = GetStatusColor(issueCount);
            var statusText = GetStatusText(issueCount);

            // Create ADF document structure
            var content = new List<object>();

            // Header with status panel
            content.Add(
                new
                {
                    type = "panel",
                    attrs = new { panelType = "info" },
                    content = new object[]
                    {
                        new
                        {
                            type = "paragraph",
                            content = new object[]
                            {
                                new
                                {
                                    type = "emoji",
                                    attrs = new
                                    {
                                        shortName = "robot",
                                        id = "1f916",
                                        text = "🤖"
                                    }
                                },
                                new
                                {
                                    type = "text",
                                    text = " ",
                                    marks = new object[] { }
                                },
                                new
                                {
                                    type = "text",
                                    text = "AI Code Review Report",
                                    marks = new object[] { new { type = "strong" } }
                                }
                            }
                        }
                    }
                }
            );

            // Status panel with color coding
            content.Add(
                new
                {
                    type = "panel",
                    attrs = new { panelType = statusColor },
                    content = new object[]
                    {
                        new
                        {
                            type = "paragraph",
                            content = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = $"Status: {statusText}",
                                    marks = new object[] { new { type = "strong" } }
                                },
                                new
                                {
                                    type = "text",
                                    text = $" • {issueCount} issues found ({severity} severity)",
                                    marks = new object[] { }
                                }
                            }
                        }
                    }
                }
            );

            // Enhanced summary table with better styling
            content.Add(
                new
                {
                    type = "table",
                    attrs = new { isNumberColumnEnabled = false, layout = "default" },
                    content = new object[]
                    {
                        new
                        {
                            type = "tableRow",
                            content = new object[]
                            {
                                new
                                {
                                    type = "tableHeader",
                                    attrs = new { background = "#f4f5f7" },
                                    content = new object[]
                                    {
                                        new
                                        {
                                            type = "paragraph",
                                            content = new object[]
                                            {
                                                new
                                                {
                                                    type = "text",
                                                    text = "📝 Pull Request",
                                                    marks = new object[] { new { type = "strong" } }
                                                }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "tableHeader",
                                    attrs = new { background = "#f4f5f7" },
                                    content = new object[]
                                    {
                                        new
                                        {
                                            type = "paragraph",
                                            content = new object[]
                                            {
                                                new
                                                {
                                                    type = "text",
                                                    text = "👤 Author",
                                                    marks = new object[] { new { type = "strong" } }
                                                }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "tableHeader",
                                    attrs = new { background = "#f4f5f7" },
                                    content = new object[]
                                    {
                                        new
                                        {
                                            type = "paragraph",
                                            content = new object[]
                                            {
                                                new
                                                {
                                                    type = "text",
                                                    text = "📁 Files",
                                                    marks = new object[] { new { type = "strong" } }
                                                }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "tableHeader",
                                    attrs = new { background = "#f4f5f7" },
                                    content = new object[]
                                    {
                                        new
                                        {
                                            type = "paragraph",
                                            content = new object[]
                                            {
                                                new
                                                {
                                                    type = "text",
                                                    text = "🔍 Issues",
                                                    marks = new object[] { new { type = "strong" } }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new
                        {
                            type = "tableRow",
                            content = new object[]
                            {
                                new
                                {
                                    type = "tableCell",
                                    attrs = new { background = "#ffffff" },
                                    content = new object[]
                                    {
                                        new
                                        {
                                            type = "paragraph",
                                            content = new object[]
                                            {
                                                new
                                                {
                                                    type = "text",
                                                    text = $"#{prNumber}",
                                                    marks = new object[] { new { type = "code" } }
                                                }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "tableCell",
                                    attrs = new { background = "#ffffff" },
                                    content = new object[]
                                    {
                                        new
                                        {
                                            type = "paragraph",
                                            content = new object[]
                                            {
                                                new
                                                {
                                                    type = "text",
                                                    text = author,
                                                    marks = new object[] { new { type = "strong" } }
                                                }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "tableCell",
                                    attrs = new { background = "#ffffff" },
                                    content = new object[]
                                    {
                                        new
                                        {
                                            type = "paragraph",
                                            content = new object[]
                                            {
                                                new
                                                {
                                                    type = "text",
                                                    text = reviewedFiles.Count.ToString()
                                                }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "tableCell",
                                    attrs = new { background = "#ffffff" },
                                    content = new object[]
                                    {
                                        new
                                        {
                                            type = "paragraph",
                                            content = new object[]
                                            {
                                                new
                                                {
                                                    type = "text",
                                                    text = $"{issueCount} ({severity})",
                                                    marks = new object[]
                                                    {
                                                        new { type = "strong" },
                                                        new
                                                        {
                                                            type = "textColor",
                                                            attrs = new
                                                            {
                                                                color = GetSeverityColor(issueCount)
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            );

            // Recommendation with enhanced styling
            content.Add(
                new
                {
                    type = "panel",
                    attrs = new { panelType = "note" },
                    content = new object[]
                    {
                        new
                        {
                            type = "paragraph",
                            content = new object[]
                            {
                                new
                                {
                                    type = "emoji",
                                    attrs = new
                                    {
                                        shortName = "dart",
                                        id = "1f3af",
                                        text = "🎯"
                                    }
                                },
                                new
                                {
                                    type = "text",
                                    text = " ",
                                    marks = new object[] { }
                                },
                                new
                                {
                                    type = "text",
                                    text = "Recommendation: ",
                                    marks = new object[] { new { type = "strong" } }
                                },
                                new
                                {
                                    type = "text",
                                    text = recommendation,
                                    marks = new object[] { new { type = "em" } }
                                }
                            }
                        }
                    }
                }
            );

            // Enhanced Top issues section with detailed formatting
            if (detailedIssues?.Any() == true)
            {
                content.Add(
                    new
                    {
                        type = "panel",
                        attrs = new { panelType = "warning" },
                        content = new object[]
                        {
                            new
                            {
                                type = "paragraph",
                                content = new object[]
                                {
                                    new
                                    {
                                        type = "emoji",
                                        attrs = new
                                        {
                                            shortName = "mag",
                                            id = "1f50d",
                                            text = "🔍"
                                        }
                                    },
                                    new
                                    {
                                        type = "text",
                                        text = " ",
                                        marks = new object[] { }
                                    },
                                    new
                                    {
                                        type = "text",
                                        text = "Detailed Issues Found:",
                                        marks = new object[] { new { type = "strong" } }
                                    }
                                }
                            }
                        }
                    }
                );

                var issueNumber = 1;
                foreach (var issue in detailedIssues.Take(5)) // Show up to 5 detailed issues
                {
                    // Issue header with severity color
                    var severityColor = issue.Severity?.ToLower() switch
                    {
                        "critical" => "#DC143C",
                        "high" => "#FF4500",
                        "medium" => "#FFA500",
                        "low" => "#32CD32",
                        _ => "#6B7280"
                    };

                    content.Add(
                        new
                        {
                            type = "panel",
                            attrs = new { panelType = "note" },
                            content = new object[]
                            {
                                new
                                {
                                    type = "paragraph",
                                    content = new object[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = $"{issueNumber}. ",
                                            marks = new object[] { new { type = "strong" } }
                                        },
                                        new
                                        {
                                            type = "text",
                                            text = issue.Title,
                                            marks = new object[] { new { type = "strong" } }
                                        },
                                        new
                                        {
                                            type = "text",
                                            text = " | ",
                                            marks = new object[] { }
                                        },
                                        new
                                        {
                                            type = "text",
                                            text = $"[{issue.Severity}]",
                                            marks = new object[]
                                            {
                                                new
                                                {
                                                    type = "textColor",
                                                    attrs = new { color = severityColor }
                                                }
                                            }
                                        },
                                        new
                                        {
                                            type = "text",
                                            text = $" | {issue.Category}",
                                            marks = new object[] { new { type = "em" } }
                                        }
                                    }
                                }
                            }
                        }
                    );

                    // Issue description
                    if (!string.IsNullOrEmpty(issue.Description))
                    {
                        content.Add(
                            new
                            {
                                type = "paragraph",
                                content = new object[]
                                {
                                    new
                                    {
                                        type = "text",
                                        text = "📋 Description: ",
                                        marks = new object[] { new { type = "strong" } }
                                    },
                                    new
                                    {
                                        type = "text",
                                        text = issue.Description,
                                        marks = new object[] { }
                                    }
                                }
                            }
                        );
                    }

                    // Recommendation
                    if (!string.IsNullOrEmpty(issue.Recommendation))
                    {
                        content.Add(
                            new
                            {
                                type = "paragraph",
                                content = new object[]
                                {
                                    new
                                    {
                                        type = "text",
                                        text = "💡 Fix: ",
                                        marks = new object[] { new { type = "strong" } }
                                    },
                                    new
                                    {
                                        type = "text",
                                        text = issue.Recommendation,
                                        marks = new object[] { new { type = "em" } }
                                    }
                                }
                            }
                        );
                    }

                    // File and line info
                    if (issue.LineNumber.HasValue)
                    {
                        content.Add(
                            new
                            {
                                type = "paragraph",
                                content = new object[]
                                {
                                    new
                                    {
                                        type = "text",
                                        text = "📍 Location: ",
                                        marks = new object[] { new { type = "strong" } }
                                    },
                                    new
                                    {
                                        type = "text",
                                        text = $"{issue.FileName} (Line {issue.LineNumber})",
                                        marks = new object[] { new { type = "code" } }
                                    }
                                }
                            }
                        );
                    }
                    else if (!string.IsNullOrEmpty(issue.FileName))
                    {
                        content.Add(
                            new
                            {
                                type = "paragraph",
                                content = new object[]
                                {
                                    new
                                    {
                                        type = "text",
                                        text = "📍 File: ",
                                        marks = new object[] { new { type = "strong" } }
                                    },
                                    new
                                    {
                                        type = "text",
                                        text = issue.FileName,
                                        marks = new object[] { new { type = "code" } }
                                    }
                                }
                            }
                        );
                    }

                    // Add separator between issues except for the last one
                    if (issueNumber < Math.Min(detailedIssues.Count, 5))
                    {
                        content.Add(new { type = "rule" });
                    }

                    issueNumber++;
                }

                // Summary if more issues exist
                if (detailedIssues.Count > 5)
                {
                    content.Add(
                        new
                        {
                            type = "paragraph",
                            content = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = $"... and {detailedIssues.Count - 5} more detailed issue(s) found",
                                    marks = new object[]
                                    {
                                        new { type = "em" },
                                        new
                                        {
                                            type = "textColor",
                                            attrs = new { color = "#6B7280" }
                                        }
                                    }
                                }
                            }
                        }
                    );
                }
            }
            else if (topIssues.Any())
            {
                // Fallback to simple issues if detailed issues not available
                content.Add(
                    new
                    {
                        type = "panel",
                        attrs = new { panelType = "warning" },
                        content = new object[]
                        {
                            new
                            {
                                type = "paragraph",
                                content = new object[]
                                {
                                    new
                                    {
                                        type = "emoji",
                                        attrs = new
                                        {
                                            shortName = "mag",
                                            id = "1f50d",
                                            text = "🔍"
                                        }
                                    },
                                    new
                                    {
                                        type = "text",
                                        text = " ",
                                        marks = new object[] { }
                                    },
                                    new
                                    {
                                        type = "text",
                                        text = "Key Issues Found:",
                                        marks = new object[] { new { type = "strong" } }
                                    }
                                }
                            }
                        }
                    }
                );

                // Create a numbered list for issues
                var issueList = new List<object>();
                var issueNumber = 1;

                foreach (var issue in topIssues.Take(3))
                {
                    issueList.Add(
                        new
                        {
                            type = "listItem",
                            content = new object[]
                            {
                                new
                                {
                                    type = "paragraph",
                                    content = new object[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = $"{issueNumber}. ",
                                            marks = new object[] { new { type = "strong" } }
                                        },
                                        new
                                        {
                                            type = "text",
                                            text = issue,
                                            marks = new object[] { }
                                        }
                                    }
                                }
                            }
                        }
                    );
                    issueNumber++;
                }

                content.Add(new { type = "orderedList", content = issueList.ToArray() });

                if (topIssues.Count > 3)
                {
                    content.Add(
                        new
                        {
                            type = "paragraph",
                            content = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = $"... and {topIssues.Count - 3} more issue(s)",
                                    marks = new object[]
                                    {
                                        new { type = "em" },
                                        new
                                        {
                                            type = "textColor",
                                            attrs = new { color = "#6B7280" }
                                        }
                                    }
                                }
                            }
                        }
                    );
                }
            }

            // Add timestamp and footer
            content.Add(new { type = "rule" });

            content.Add(
                new
                {
                    type = "paragraph",
                    content = new object[]
                    {
                        new
                        {
                            type = "emoji",
                            attrs = new
                            {
                                shortName = "clock",
                                id = "1f551",
                                text = "🕑"
                            }
                        },
                        new
                        {
                            type = "text",
                            text = " ",
                            marks = new object[] { }
                        },
                        new
                        {
                            type = "text",
                            text = "Generated on: ",
                            marks = new object[] { new { type = "em" } }
                        },
                        new
                        {
                            type = "text",
                            text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                            marks = new object[] { new { type = "code" } }
                        },
                        new
                        {
                            type = "text",
                            text = " by AI Code Reviewer",
                            marks = new object[] { new { type = "em" } }
                        }
                    }
                }
            );

            return new
            {
                type = "doc",
                version = 1,
                content = content.ToArray()
            };
        }

        /// <summary>
        /// Creates a formatted comment for Jira tickets (legacy method for backward compatibility)
        /// </summary>
        public string CreateJiraComment(
            string prNumber,
            string author,
            int issueCount,
            List<string> reviewedFiles,
            List<string> topIssues
        )
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
