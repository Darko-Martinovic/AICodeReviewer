using AICodeReviewer.Services;

namespace AICodeReviewer.Application
{
    /// <summary>
    /// Application layer service that orchestrates the code review workflow
    /// </summary>
    public class CodeReviewApplication
    {
        private readonly GitHubService _gitHubService;
        private readonly CodeReviewService _codeReviewService;
        private readonly NotificationService _notificationService;
        private readonly JiraService _jiraService;

        public CodeReviewApplication(
            GitHubService gitHubService,
            CodeReviewService codeReviewService,
            NotificationService notificationService,
            JiraService jiraService)
        {
            _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
            _codeReviewService = codeReviewService ?? throw new ArgumentNullException(nameof(codeReviewService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _jiraService = jiraService ?? throw new ArgumentNullException(nameof(jiraService));
        }

        /// <summary>
        /// Reviews the latest commit on the main branch
        /// </summary>
        public async Task ReviewLatestCommitAsync()
        {
            try
            {
                Console.WriteLine("🔍 Fetching latest commit...");
                Console.WriteLine("──────────────────────────────────────────────────────────────");

                // Get repository info
                var repoInfo = await GetRepositoryInfoAsync();
                Console.WriteLine($"🏠 Repository: {repoInfo.Owner}/{repoInfo.Name}");
                Console.WriteLine($"🌿 Branch: main");
                Console.WriteLine();

                // Get latest commit from main branch
                var commits = await _gitHubService.GetCommitsAsync();

                if (!commits.Any())
                {
                    Console.WriteLine("❌ No commits found.\n");
                    return;
                }

                var latestCommit = commits.First();
                Console.WriteLine($"📝 Latest commit: {latestCommit.Sha[..8]} - {latestCommit.Commit.Message}");
                Console.WriteLine($"👤 Author: {latestCommit.Commit.Author.Name}");
                Console.WriteLine($"📅 Date: {latestCommit.Commit.Author.Date:yyyy-MM-dd HH:mm}");

                // Get commit details with file changes
                var commitDetail = await _gitHubService.GetCommitDetailAsync(latestCommit.Sha);

                Console.WriteLine($"\n📁 Files changed: {commitDetail.Files.Count}");
                foreach (var file in commitDetail.Files)
                {
                    Console.WriteLine($"  - {file.Status}: {file.Filename} (+{file.Additions}/-{file.Deletions})");
                }

                // AI Code Review
                Console.WriteLine("\n🤖 Starting AI Code Review...");
                var reviewResult = await _codeReviewService.ReviewCommitAsync(commitDetail.Files);

                // Send Teams notification
                await _notificationService.SendTeamsNotificationAsync(
                    latestCommit.Sha,
                    latestCommit.Commit.Author.Name,
                    reviewResult.ReviewedFiles,
                    reviewResult.IssueCount,
                    reviewResult.AllIssues
                );

                Console.WriteLine("──────────────────────────────────────────────────────────────\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reviewing commit: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Shows the pull request review menu and handles user selection
        /// </summary>
        public async Task ReviewPullRequestAsync()
        {
            try
            {
                Console.WriteLine("🔍 Fetching open Pull Requests...");
                Console.WriteLine("──────────────────────────────────────────────────────────────");

                // Get repository info
                var repoInfo = await GetRepositoryInfoAsync();
                Console.WriteLine($"🏠 Repository: {repoInfo.Owner}/{repoInfo.Name}");

                var pullRequests = await _gitHubService.GetOpenPullRequestsAsync();

                if (!pullRequests.Any())
                {
                    Console.WriteLine("❌ No open Pull Requests found.\n");
                    return;
                }

                // Auto-select if only one PR
                if (pullRequests.Count == 1)
                {
                    var singlePr = pullRequests[0];
                    Console.WriteLine($"\n✅ Found 1 Pull Request:");
                    Console.WriteLine($"   PR #{singlePr.Number}: {singlePr.Title}");
                    Console.WriteLine($"   🌿 {singlePr.Head.Ref} → {singlePr.Base.Ref}");
                    Console.WriteLine("   Auto-selecting for review...\n");
                    await ReviewSpecificPullRequestAsync(singlePr);
                    return;
                }

                // Show menu for multiple PRs
                Console.WriteLine($"\n📋 Open Pull Requests ({pullRequests.Count} found):");
                for (int i = 0; i < pullRequests.Count; i++)
                {
                    var pr = pullRequests[i];
                    Console.WriteLine($"{i + 1}. PR #{pr.Number}: {pr.Title}");
                    Console.WriteLine($"   👤 {pr.User.Login} | 🌿 {pr.Head.Ref} → {pr.Base.Ref}");
                }

                Console.Write($"\nEnter selection (1-{pullRequests.Count}) or 0 to cancel: ");
                if (int.TryParse(Console.ReadLine(), out int selection))
                {
                    if (selection == 0)
                    {
                        Console.WriteLine("❌ Cancelled.\n");
                        return;
                    }

                    if (selection >= 1 && selection <= pullRequests.Count)
                    {
                        var selectedPr = pullRequests[selection - 1]; // Convert to 0-based index
                        await ReviewSpecificPullRequestAsync(selectedPr);
                    }
                    else
                    {
                        Console.WriteLine($"❌ Invalid selection. Please enter a number between 1 and {pullRequests.Count}.\n");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Invalid input. Please enter a number.\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reviewing PR: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Reviews a specific pull request
        /// </summary>
        private async Task ReviewSpecificPullRequestAsync(Octokit.PullRequest pr)
        {
            Console.WriteLine($"\n📋 Reviewing PR #{pr.Number}: {pr.Title}");

            // Get repository info
            var repoInfo = await GetRepositoryInfoAsync();
            Console.WriteLine($"🏠 Repository: {repoInfo.Owner}/{repoInfo.Name}");
            Console.WriteLine($"👤 Author: {pr.User.Login}");
            Console.WriteLine($"🌿 Branch: {pr.Head.Ref} → {pr.Base.Ref}");

            // Extract Jira ticket keys from PR title
            var jiraTickets = _jiraService.ExtractTicketKeysFromTitle(pr.Title);
            if (jiraTickets.Any())
            {
                Console.WriteLine($"🎫 Detected Jira tickets: {string.Join(", ", jiraTickets)}");
            }
            else
            {
                Console.WriteLine($"🎫 No Jira tickets detected in PR title: \"{pr.Title}\"");
            }

            // Get PR files
            var prFiles = await _gitHubService.GetPullRequestFilesAsync(pr.Number);

            Console.WriteLine($"\n📁 Files changed: {prFiles.Count}");
            foreach (var file in prFiles)
            {
                Console.WriteLine($"  - {file.Status}: {file.FileName} (+{file.Additions}/-{file.Deletions})");
            }

            // AI Code Review
            Console.WriteLine("\n🤖 Starting AI Code Review...");
            var reviewResult = await _codeReviewService.ReviewPullRequestAsync(prFiles);

            // Send Teams notification for PR
            await _notificationService.SendTeamsNotificationAsync(
                pr.Number.ToString(),
                pr.User.Login,
                reviewResult.ReviewedFiles,
                reviewResult.IssueCount,
                reviewResult.AllIssues
            );

            // Update Jira tickets with review results
            Console.WriteLine("\n🎫 JIRA Ticket Updates:");
            Console.WriteLine("──────────────────────────────────────────────────────────────");
            await _jiraService.UpdateTicketsWithReviewResultsAsync(
                jiraTickets,
                pr.Number.ToString(),
                pr.User.Login,
                reviewResult.IssueCount,
                reviewResult.ReviewedFiles,
                reviewResult.AllIssues.Take(5).ToList()
            );

            // Add PR comment simulation
            Console.WriteLine("\n💬 GitHub PR Comment:");
            Console.WriteLine("──────────────────────────────────────────────────────────────");
            await SimulatePRCommentAsync(pr.Number, reviewResult);

            Console.WriteLine("──────────────────────────────────────────────────────────────\n");
        }

        /// <summary>
        /// Simulates posting a PR comment with review results
        /// </summary>
        private async Task SimulatePRCommentAsync(int prNumber, Models.CodeReviewResult reviewResult)
        {
            Console.WriteLine("\n💬 GitHub PR Comment:");
            Console.WriteLine("──────────────────────────────────────────────────────────────");

            // Create the comment content
            var comment = CreatePRComment(reviewResult);

            // Try to post real comment first
            var success = await _gitHubService.PostPullRequestCommentAsync(prNumber, comment);

            if (success)
            {
                Console.WriteLine("   ✅ Successfully posted comment to GitHub PR");
            }
            else
            {
                Console.WriteLine("   ⚠️  Failed to post to GitHub - showing simulated comment:");
            }

            // Always show the comment content for visibility
            Console.WriteLine("📝 Comment Content:");
            Console.WriteLine(comment);
            Console.WriteLine("──────────────────────────────────────────────────────────────");
        }

        /// <summary>
        /// Creates a formatted PR comment
        /// </summary>
        private string CreatePRComment(Models.CodeReviewResult reviewResult)
        {
            var comment = "## 🤖 AI Code Review Summary\n\n";

            // Review summary
            comment += $"📊 **Files Reviewed:** {reviewResult.ReviewedFiles.Count}\n";
            comment += $"🔍 **Issues Found:** {reviewResult.IssueCount}\n";

            string severity = reviewResult.IssueCount switch
            {
                0 => "✅ Clean",
                <= 2 => "⚠️ Low",
                <= 5 => "🔶 Medium",
                _ => "🚨 High"
            };
            comment += $"📈 **Severity:** {severity}\n\n";

            // List files reviewed
            if (reviewResult.ReviewedFiles.Any())
            {
                comment += "📁 **Files Analyzed:**\n";
                foreach (var file in reviewResult.ReviewedFiles.Take(10)) // Limit to first 10 files
                {
                    comment += $"• `{file}`\n";
                }
                if (reviewResult.ReviewedFiles.Count > 10)
                {
                    comment += $"• *...and {reviewResult.ReviewedFiles.Count - 10} more files*\n";
                }
                comment += "\n";
            }

            // Show top issues
            if (reviewResult.AllIssues.Any())
            {
                comment += "🔍 **Key Issues Identified:**\n";
                var topIssues = reviewResult.AllIssues.Take(5);
                int issueNumber = 1;
                foreach (var issue in topIssues)
                {
                    comment += $"{issueNumber}. {issue}\n";
                    issueNumber++;
                }

                if (reviewResult.AllIssues.Count > 5)
                {
                    comment += $"*...and {reviewResult.AllIssues.Count - 5} more issues*\n";
                }
                comment += "\n";
            }

            // Recommendations
            comment += "🎯 **Recommendations:**\n";
            if (reviewResult.IssueCount == 0)
            {
                comment += "✅ Code looks good! Ready to merge.\n";
            }
            else if (reviewResult.IssueCount <= 2)
            {
                comment += "⚠️ Minor issues found. Consider reviewing before merge.\n";
            }
            else if (reviewResult.IssueCount <= 5)
            {
                comment += "🔶 Several issues identified. Please review and address.\n";
            }
            else
            {
                comment += "🚨 Multiple issues found. Recommend fixes before merge.\n";
            }

            comment += "\n---\n*Generated by AI Code Reviewer* 🤖";
            return comment;
        }

        /// <summary>
        /// Lists recent commits
        /// </summary>
        public async Task ListRecentCommitsAsync()
        {
            try
            {
                Console.WriteLine("📝 Recent commits:");
                Console.WriteLine("──────────────────────────────────────────────────────────────");

                var commits = await _gitHubService.GetCommitsAsync();
                var recentCommits = commits.Take(5);

                if (!recentCommits.Any())
                {
                    Console.WriteLine("  No commits found.");
                    Console.WriteLine("──────────────────────────────────────────────────────────────\n");
                    return;
                }

                // Get repository info from GitHubService (we'll need to add a method for this)
                var repoInfo = await GetRepositoryInfoAsync();

                Console.WriteLine($"🏠 Repository: {repoInfo.Owner}/{repoInfo.Name}");
                Console.WriteLine($"🌿 Branch: main");
                Console.WriteLine($"📊 Total commits: {commits.Count}");
                Console.WriteLine();

                int commitNumber = 1;
                foreach (var commit in recentCommits)
                {
                    var date = commit.Commit.Author.Date.ToString("yyyy-MM-dd HH:mm");
                    var shortSha = commit.Sha[..8];
                    var message = commit.Commit.Message.Length > 60
                        ? commit.Commit.Message[..57] + "..."
                        : commit.Commit.Message;

                    Console.WriteLine($"  {commitNumber}. 🔗 {shortSha}");
                    Console.WriteLine($"     📝 {message}");
                    Console.WriteLine($"     👤 {commit.Commit.Author.Name}");
                    Console.WriteLine($"     📅 {date}");
                    Console.WriteLine();
                    commitNumber++;
                }

                Console.WriteLine("──────────────────────────────────────────────────────────────\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching commits: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Lists open pull requests
        /// </summary>
        public async Task ListOpenPullRequestsAsync()
        {
            try
            {
                Console.WriteLine("🔀 Open Pull Requests:");
                Console.WriteLine("──────────────────────────────────────────────────────────────");

                var pullRequests = await _gitHubService.GetOpenPullRequestsAsync();

                if (!pullRequests.Any())
                {
                    Console.WriteLine("  No open Pull Requests found.");
                    Console.WriteLine("──────────────────────────────────────────────────────────────\n");
                    return;
                }

                // Get repository info
                var repoInfo = await GetRepositoryInfoAsync();

                Console.WriteLine($"🏠 Repository: {repoInfo.Owner}/{repoInfo.Name}");
                Console.WriteLine($"📊 Total open PRs: {pullRequests.Count}");
                Console.WriteLine();

                int prNumber = 1;
                foreach (var pr in pullRequests)
                {
                    var title = pr.Title.Length > 50
                        ? pr.Title[..47] + "..."
                        : pr.Title;
                    var createdAt = pr.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                    var updatedAt = pr.UpdatedAt.ToString("yyyy-MM-dd HH:mm");

                    Console.WriteLine($"  {prNumber}. 🔀 PR #{pr.Number}");
                    Console.WriteLine($"     📝 {title}");
                    Console.WriteLine($"     👤 {pr.User.Login}");
                    Console.WriteLine($"     🌿 {pr.Head.Ref} → {pr.Base.Ref}");
                    Console.WriteLine($"     📅 Created: {createdAt}");
                    Console.WriteLine($"     🔄 Updated: {updatedAt}");

                    // Show PR status indicators
                    var statusIndicators = new List<string>();
                    if (pr.Draft) statusIndicators.Add("📋 Draft");
                    if (pr.Merged) statusIndicators.Add("✅ Merged");
                    if (pr.ClosedAt.HasValue) statusIndicators.Add("❌ Closed");

                    if (statusIndicators.Any())
                    {
                        Console.WriteLine($"     🏷️  {string.Join(" | ", statusIndicators)}");
                    }

                    Console.WriteLine();
                    prNumber++;
                }

                Console.WriteLine("──────────────────────────────────────────────────────────────\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching PRs: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Gets repository information
        /// </summary>
        private async Task<(string Owner, string Name)> GetRepositoryInfoAsync()
        {
            // For now, we'll get this from the GitHubService
            // In a more robust implementation, we could add a method to GitHubService to get repo details
            var repoDetails = await _gitHubService.GetRepositoryDetailsAsync();
            return (repoDetails.Owner.Login, repoDetails.Name);
        }
    }
}
