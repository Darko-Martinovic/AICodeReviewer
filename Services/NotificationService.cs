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
            // Simulate webhook preparation and sending
            Console.WriteLine("📡 Preparing Teams notification...");
            await Task.Delay(500); // Simulate network delay
            
            Console.WriteLine("🔗 Connecting to Teams webhook...");
            await Task.Delay(300);
            
            Console.WriteLine("📤 Sending notification to Teams channel...");
            await Task.Delay(400);

            // Determine severity
            string severity = GetSeverityText(issueCount);

            // Handle both commit SHAs and PR numbers
            string displayId = commitSha.Length >= 8 ? commitSha[..8] : $"PR #{commitSha}";

            // Simulate Teams channel message display
            Console.WriteLine();
            Console.WriteLine("💬 Microsoft Teams - Code Review Channel");
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
            
            // Simulate delivery confirmation and team interactions
            await SimulateTeamsInteractionsAsync(issueCount, author);
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

        /// <summary>
        /// Simulates Teams user interactions and delivery confirmations
        /// </summary>
        private async Task SimulateTeamsInteractionsAsync(int issueCount, string author)
        {
            Console.WriteLine();
            
            // Simulate delivery confirmation
            await Task.Delay(200);
            Console.WriteLine("✅ Message delivered to Teams channel");
            
            // Simulate some realistic delays for user interactions
            await Task.Delay(800);
            
            // Simulate reactions based on severity
            var reactions = GetReactionsBasedOnSeverity(issueCount);
            Console.WriteLine($"👥 Team reactions: {string.Join(" ", reactions)}");
            
            await Task.Delay(600);
            
            // Simulate team member responses
            var teamMembers = new[] { "Alice Johnson", "Bob Chen", "Carol Smith", "Dave Wilson" };
            var responseCount = new Random().Next(1, 3); // 1-2 responses
            
            for (int i = 0; i < responseCount; i++)
            {
                var member = teamMembers[new Random().Next(teamMembers.Length)];
                var response = GetRandomTeamResponse(issueCount, author, member);
                
                await Task.Delay(new Random().Next(500, 1200));
                Console.WriteLine($"💬 {member}: {response}");
            }
            
            // Simulate mention notifications
            await Task.Delay(400);
            if (issueCount > 5)
            {
                Console.WriteLine($"🔔 @{author} has been mentioned in this conversation");
            }
            
            // Simulate webhook delivery status
            await Task.Delay(300);
            Console.WriteLine("📊 Teams notification metrics: Delivered ✅ | Read by 4 team members 👀");
        }

        /// <summary>
        /// Gets reactions based on code review severity
        /// </summary>
        private static string[] GetReactionsBasedOnSeverity(int issueCount)
        {
            return issueCount switch
            {
                0 => new[] { "👍", "🎉", "✅", "🚀" },
                <= 2 => new[] { "👍", "⚠️", "👀" },
                <= 5 => new[] { "😬", "⚠️", "🔧", "👀" },
                _ => new[] { "😱", "🚨", "🔧", "⚠️", "😟" }
            };
        }

        /// <summary>
        /// Generates random but contextual team member responses
        /// </summary>
        private static string GetRandomTeamResponse(int issueCount, string author, string memberName)
        {
            var responses = issueCount switch
            {
                0 => new[]
                {
                    "Great work! Clean code 👌",
                    "LGTM! Ready to merge 🚀",
                    "Nice job on the implementation!",
                    "Code looks solid ✅"
                },
                <= 2 => new[]
                {
                    "Just minor issues, quick fixes needed",
                    "Almost there! Small tweaks and we're good 👍",
                    "Good work overall, just address those minor points",
                    "Looking good, please fix the small issues"
                },
                <= 5 => new[]
                {
                    "Several things to address before merge",
                    "Please review the feedback and update",
                    "Some important improvements needed 🔧",
                    "Let's iterate on these issues together"
                },
                _ => new[]
                {
                    $"@{author} Please address the critical issues ASAP 🚨",
                    "This needs significant work before we can proceed",
                    "Multiple blockers - let's schedule a review session",
                    "Critical issues found - please prioritize fixes"
                }
            };

            return responses[new Random().Next(responses.Length)];
        }
    }
}
