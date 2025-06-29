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

            Console.WriteLine("📤 Teams Notification Sent:");
            Console.WriteLine("══════════════════════════════════════════════════════════════");
            Console.WriteLine($"🤖 AI Code Review: {commitSha[..8]}");
            Console.WriteLine($"👤 Author: {author}");
            Console.WriteLine($"📁 Files: {reviewedFiles.Count} code files reviewed");
            Console.WriteLine($"🔍 Issues: {issueCount} found ({severity})");

            if (topIssues.Any())
            {
                Console.WriteLine("\n📋 Top Issues:");
                var issuesToShow = topIssues.Take(3);
                foreach (var issue in issuesToShow)
                {
                    Console.WriteLine($"  • {issue}");
                }

                if (topIssues.Count > 3)
                {
                    Console.WriteLine($"  ... and {topIssues.Count - 3} more issue(s)");
                }
            }

            Console.WriteLine("══════════════════════════════════════════════════════════════");
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
