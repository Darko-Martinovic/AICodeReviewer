using System;
using System.Linq;
using System.Threading.Tasks;
using AICodeReviewer.Services;
using AICodeReviewer.Models;
using AICodeReviewer.Models.Configuration;

namespace AICodeReviewer.Application
{
    /// <summary>
    /// Main application class that orchestrates the code review process
    /// </summary>
    public class CodeReviewApplication
    {
        private readonly IGitHubService _gitHubService;
        private readonly ICodeReviewService _codeReviewService;
        private readonly INotificationService _notificationService;
        private readonly IJiraService _jiraService;
        private readonly IRepositoryManagementService _repositoryManagementService;
        private bool _repositorySelected = true; // Set to true initially since we have a default repository

        public CodeReviewApplication(
            IGitHubService gitHubService,
            ICodeReviewService codeReviewService,
            INotificationService notificationService,
            IJiraService jiraService,
            IRepositoryManagementService repositoryManagementService
        )
        {
            _gitHubService = gitHubService;
            _codeReviewService = codeReviewService;
            _notificationService = notificationService;
            _jiraService = jiraService;
            _repositoryManagementService = repositoryManagementService;
        }

        /// <summary>
        /// Ensures a repository is selected, prompting only if necessary
        /// </summary>
        private async Task<(string Owner, string Name)> EnsureRepositorySelectedAsync()
        {
            // If repository is already selected, just return the current one
            if (_repositorySelected)
            {
                var currentRepo = await _repositoryManagementService.GetCurrentRepositoryAsync();
                _gitHubService.UpdateRepository(currentRepo.Owner, currentRepo.Name);
                return currentRepo;
            }

            // Otherwise, prompt for repository selection
            var (owner, name) = await _repositoryManagementService.PromptForRepositoryAsync();
            _gitHubService.UpdateRepository(owner, name);
            _repositorySelected = true;
            return (owner, name);
        }

        /// <summary>
        /// Reviews the latest commit (simulating a push event)
        /// </summary>
        public async Task ReviewLatestCommitAsync()
        {
            try
            {
                // Ensure repository is selected
                var (owner, name) = await EnsureRepositorySelectedAsync();

                Console.WriteLine("🔍 Review Latest Commit (Push Event)");
                Console.WriteLine("──────────────────────────────────────────────────────────────");

                // Get repository info
                var repoInfo = await GetRepositoryInfoAsync();
                Console.WriteLine($"🏠 Repository: {repoInfo.Owner}/{repoInfo.Name}");

                // Get latest commits
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

                // Get detailed commit info with file changes
                var commitDetail = await _gitHubService.GetCommitDetailAsync(latestCommit.Sha);

                Console.WriteLine($"\n📁 Files changed: {commitDetail.Files.Count}");
                foreach (var file in commitDetail.Files)
                {
                    Console.WriteLine(
                        $"  - {file.Status}: {file.Filename} (+{file.Additions}/-{file.Deletions})"
                    );
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

                Console.WriteLine(
                    "──────────────────────────────────────────────────────────────\n"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reviewing latest commit: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Reviews a specific commit by hash
        /// </summary>
        public async Task ReviewCommitByHashAsync()
        {
            try
            {
                // Ensure repository is selected
                var (owner, name) = await EnsureRepositorySelectedAsync();

                Console.WriteLine("🔍 Review Commit by Hash");
                Console.WriteLine("──────────────────────────────────────────────────────────────");

                // Get repository info
                var repoInfo = await GetRepositoryInfoAsync();
                Console.WriteLine($"🏠 Repository: {repoInfo.Owner}/{repoInfo.Name}");

                Console.Write("Enter commit hash (full or short): ");
                var commitHash = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(commitHash))
                {
                    Console.WriteLine("❌ No commit hash provided.\n");
                    return;
                }

                Console.WriteLine($"\n🔍 Fetching commit details for: {commitHash}");

                // Get commit details with file changes
                var commitDetail = await _gitHubService.GetCommitDetailAsync(commitHash);

                if (commitDetail == null)
                {
                    Console.WriteLine("❌ Commit not found. Please check the hash and try again.\n");
                    return;
                }

                Console.WriteLine($"📝 Commit: {commitDetail.Sha[..8]} - {commitDetail.Commit.Message}");
                Console.WriteLine($"👤 Author: {commitDetail.Commit.Author.Name}");
                Console.WriteLine($"📅 Date: {commitDetail.Commit.Author.Date:yyyy-MM-dd HH:mm}");

                Console.WriteLine($"\n📁 Files changed: {commitDetail.Files.Count}");
                foreach (var file in commitDetail.Files)
                {
                    Console.WriteLine(
                        $"  - {file.Status}: {file.Filename} (+{file.Additions}/-{file.Deletions})"
                    );
                }

                // AI Code Review
                Console.WriteLine("\n🤖 Starting AI Code Review...");
                var reviewResult = await _codeReviewService.ReviewCommitAsync(commitDetail.Files);

                // Send Teams notification
                await _notificationService.SendTeamsNotificationAsync(
                    commitDetail.Sha,
                    commitDetail.Commit.Author.Name,
                    reviewResult.ReviewedFiles,
                    reviewResult.IssueCount,
                    reviewResult.AllIssues
                );

                Console.WriteLine(
                    "──────────────────────────────────────────────────────────────\n"
                );
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
                // Ensure repository is selected
                var (owner, name) = await EnsureRepositorySelectedAsync();

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
                        Console.WriteLine(
                            $"❌ Invalid selection. Please enter a number between 1 and {pullRequests.Count}.\n"
                        );
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
                Console.WriteLine(
                    $"  - {file.Status}: {file.FileName} (+{file.Additions}/-{file.Deletions})"
                );
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
                reviewResult.AllIssues.Take(5).ToList(),
                reviewResult.DetailedIssues
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
        private async Task SimulatePRCommentAsync(
            int prNumber,
            Models.CodeReviewResult reviewResult
        )
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
                comment += "✅ Code looks good! No issues found.\n";
            }
            else
            {
                comment += "🔧 Please review the issues above and address them before merging.\n";
                comment += "💡 Consider running additional tests to ensure stability.\n";
            }

            comment += "\n---\n";
            comment += "*This review was performed by AI Code Reviewer 🤖*";

            return comment;
        }

        /// <summary>
        /// Lists recent commits for the repository
        /// </summary>
        public async Task ListRecentCommitsAsync()
        {
            try
            {
                // Ensure repository is selected
                var (owner, name) = await EnsureRepositorySelectedAsync();

                Console.WriteLine("📝 Recent Commits");
                Console.WriteLine("──────────────────────────────────────────────────────────────");

                // Get repository info
                var repoInfo = await GetRepositoryInfoAsync();
                Console.WriteLine($"🏠 Repository: {repoInfo.Owner}/{repoInfo.Name}");

                var commits = await _gitHubService.GetCommitsAsync();

                if (!commits.Any())
                {
                    Console.WriteLine("❌ No commits found.\n");
                    return;
                }

                Console.WriteLine($"\n📝 Recent commits ({commits.Count} found):");
                for (int i = 0; i < commits.Count; i++)
                {
                    var commit = commits[i];
                    Console.WriteLine($"{i + 1}. {commit.Sha[..8]} - {commit.Commit.Message}");
                    Console.WriteLine($"   👤 {commit.Commit.Author.Name} | 📅 {commit.Commit.Author.Date:yyyy-MM-dd HH:mm}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listing commits: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Lists open pull requests for the repository
        /// </summary>
        public async Task ListOpenPullRequestsAsync()
        {
            try
            {
                // Ensure repository is selected
                var (owner, name) = await EnsureRepositorySelectedAsync();

                Console.WriteLine("📋 Open Pull Requests");
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

                Console.WriteLine($"\n📋 Open Pull Requests ({pullRequests.Count} found):");
                for (int i = 0; i < pullRequests.Count; i++)
                {
                    var pr = pullRequests[i];
                    Console.WriteLine($"{i + 1}. PR #{pr.Number}: {pr.Title}");
                    Console.WriteLine($"   👤 {pr.User.Login} | 🌿 {pr.Head.Ref} → {pr.Base.Ref}");
                    Console.WriteLine($"   📅 Created: {pr.CreatedAt:yyyy-MM-dd HH:mm}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listing pull requests: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Manages repository selection and settings
        /// </summary>
        public async Task ManageRepositoriesAsync()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("🏠 Repository Management");
                    Console.WriteLine("──────────────────────────────────────────────────────────────");

                    var currentRepo = await _repositoryManagementService.GetCurrentRepositoryAsync();
                    Console.WriteLine($"📍 Current repository: {currentRepo.Owner}/{currentRepo.Name}");

                    var history = await _repositoryManagementService.GetRepositoryHistoryAsync();
                    Console.WriteLine($"\n📚 Repository history ({history.Count} repositories):");

                    for (int i = 0; i < history.Count; i++)
                    {
                        var repo = history[i];
                        var currentIndicator = (repo.Owner == currentRepo.Owner && repo.Name == currentRepo.Name) ? "📍 " : "   ";
                        Console.WriteLine($"{currentIndicator}{i + 1}. {repo.FullName}");
                        if (!string.IsNullOrEmpty(repo.Description))
                        {
                            Console.WriteLine($"     {repo.Description}");
                        }
                    }

                    Console.WriteLine("\nOptions:");
                    Console.WriteLine("  1. 🔄 Change current repository");
                    Console.WriteLine("  2. 📚 Select from available repositories");
                    Console.WriteLine("  3. 🔙 Back to main menu");
                    Console.WriteLine();

                    Console.Write("Enter your choice (1-3): ");
                    var choice = Console.ReadLine()?.Trim();

                    switch (choice)
                    {
                        case "1":
                            await _repositoryManagementService.PromptForRepositoryAsync();
                            _repositorySelected = true; // Mark that a repository has been selected
                            Console.WriteLine(); // Add spacing after repository change
                            break;
                        case "2":
                            // Get available repositories and allow selection
                            var availableRepos = await _repositoryManagementService.GetAvailableRepositoriesAsync();
                            if (!availableRepos.Any())
                            {
                                Console.WriteLine("❌ No repositories found or accessible.\n");
                                break;
                            }

                            Console.WriteLine($"\n📚 Available repositories ({availableRepos.Count} found):");
                            for (int i = 0; i < availableRepos.Count; i++)
                            {
                                var repo = availableRepos[i];
                                var currentIndicator = (repo.Owner == currentRepo.Owner && repo.Name == currentRepo.Name) ? "📍 " : "   ";
                                Console.WriteLine($"{currentIndicator}{i + 1}. {repo.FullName} ({(repo.IsPrivate ? "private" : "public")})");
                                if (!string.IsNullOrEmpty(repo.Description))
                                {
                                    Console.WriteLine($"     {repo.Description}");
                                }
                            }

                            Console.WriteLine("\n💡 Select a repository to switch to it:");
                            Console.Write($"Enter selection (1-{availableRepos.Count}) or 0 to cancel: ");
                            if (int.TryParse(Console.ReadLine(), out int selection))
                            {
                                if (selection == 0)
                                {
                                    Console.WriteLine("❌ Cancelled.\n");
                                }
                                else if (selection >= 1 && selection <= availableRepos.Count)
                                {
                                    var selectedRepo = availableRepos[selection - 1];
                                    // Update the GitHub service with the new repository
                                    _gitHubService.UpdateRepository(selectedRepo.Owner, selectedRepo.Name);
                                    await _repositoryManagementService.AddToHistoryAsync(selectedRepo.Owner, selectedRepo.Name, selectedRepo.Description);
                                    _repositorySelected = true; // Mark that a repository has been selected
                                    Console.WriteLine($"✅ Selected repository: {selectedRepo.FullName}\n");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Invalid selection. Please enter a number between 1 and {availableRepos.Count}.\n");
                                }
                            }
                            else
                            {
                                Console.WriteLine("❌ Invalid input. Please enter a number.\n");
                            }
                            break;
                        case "3":
                            Console.WriteLine("🔙 Returning to main menu...\n");
                            return;
                        default:
                            Console.WriteLine("❌ Invalid choice. Please try again.\n");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error managing repositories: {ex.Message}\n");
                }
            }
        }

        /// <summary>
        /// Gets repository information
        /// </summary>
        private async Task<(string Owner, string Name)> GetRepositoryInfoAsync()
        {
            return await _gitHubService.GetRepositoryInfoAsync();
        }
    }
}
