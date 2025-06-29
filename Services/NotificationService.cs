namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for sending notifications (simulated Teams notifications)
    /// </summary>
    public class NotificationService
    {
        /// <summary>
        /// Sends a Teams notification with code review results
        /// </summary>
        public async Task SendTeamsNotificationAsync(string commitSha, string author, List<string> reviewedFiles, int issueCount, List<string> topIssues)
        {
            await Task.Delay(1); // Simulate async operation

            // Determine severity
            string severity = GetSeverityText(issueCount);

            // Handle both commit SHAs and PR numbers
            string displayId = commitSha.Length >= 8 ? commitSha[..8] : $"PR #{commitSha}";

            Console.WriteLine("\n� Microsoft Teams - Code Review Channel");
            Console.WriteLine("════════════════════════════════════════════════════════════════════════");
            Console.WriteLine("🤖 **AI Code Reviewer** _(Bot)_ • " + DateTime.Now.ToString("MMM dd, yyyy h:mm tt"));
            Console.WriteLine();
            Console.WriteLine($"### 🔍 Code Review Complete: {displayId}");
            Console.WriteLine();
            Console.WriteLine($"**👤 Author:** {author}");
            Console.WriteLine($"**📁 Files Reviewed:** {reviewedFiles.Count} code files");
            Console.WriteLine($"**🔍 Issues Found:** {issueCount} ({severity})");
            
            // Add severity-based emoji and color indication
            string statusEmoji = issueCount switch
            {
                0 => "✅",
                <= 2 => "⚠️",
                <= 5 => "🔶",
                _ => "🚨"
            };
            
            Console.WriteLine($"**📊 Review Status:** {statusEmoji} {severity}");

            if (topIssues.Any())
            {
                Console.WriteLine();
                Console.WriteLine("**📋 Key Issues Identified:**");
                var issuesToShow = topIssues.Take(3);
                foreach (var issue in issuesToShow)
                {
                    Console.WriteLine($"• {issue}");
                }

                if (topIssues.Count > 3)
                {
                    Console.WriteLine($"• *...and {topIssues.Count - 3} more issue(s)*");
                }
            }

            // Add action buttons simulation
            Console.WriteLine();
            Console.WriteLine("**🎯 Recommended Actions:**");
            if (issueCount == 0)
            {
                Console.WriteLine("✅ **Ready to merge** - No issues found");
            }
            else if (issueCount <= 2)
            {
                Console.WriteLine("⚠️ **Review recommended** - Minor issues to address");
            }
            else if (issueCount <= 5)
            {
                Console.WriteLine("🔶 **Changes suggested** - Several issues need attention");
            }
            else
            {
                Console.WriteLine("🚨 **Fixes required** - Multiple critical issues found");
            }

            Console.WriteLine();
            Console.WriteLine("💬 [View Full Report] 🔗 [Open PR] 📋 [View in Jira]");
            Console.WriteLine("════════════════════════════════════════════════════════════════════════");
        }

        private static string GetSeverityText(int issueCount)
        {
            return issueCount switch
            {
                0 => "✅ No issues",
                <= 2 => "Low severity",
                <= 5 => "Medium severity",
                _ => "High severity"
            };
        }
    }
}
