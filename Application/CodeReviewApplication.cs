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

        public CodeReviewApplication(
            GitHubService gitHubService,
            CodeReviewService codeReviewService,
            NotificationService notificationService)
        {
            _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
            _codeReviewService = codeReviewService ?? throw new ArgumentNullException(nameof(codeReviewService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <summary>
        /// Reviews the latest commit on the main branch
        /// </summary>
        public async Task ReviewLatestCommitAsync()
        {
            try
            {
                Console.WriteLine("🔍 Fetching latest commit...");

                // Get latest commit from main branch
                var commits = await _gitHubService.GetCommitsAsync();

                if (!commits.Any())
                {
                    Console.WriteLine("No commits found.\n");
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

                Console.WriteLine();
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

                var pullRequests = await _gitHubService.GetOpenPullRequestsAsync();

                if (!pullRequests.Any())
                {
                    Console.WriteLine("No open Pull Requests found.\n");
                    return;
                }

                Console.WriteLine("\nOpen Pull Requests:");
                for (int i = 0; i < pullRequests.Count; i++)
                {
                    var pr = pullRequests[i];
                    Console.WriteLine($"{i + 1}. PR #{pr.Number}: {pr.Title}");
                }

                Console.Write("\nEnter PR number to review (or 0 to cancel): ");
                if (int.TryParse(Console.ReadLine(), out int prNumber) && prNumber > 0)
                {
                    var selectedPr = pullRequests.FirstOrDefault(pr => pr.Number == prNumber);
                    if (selectedPr != null)
                    {
                        await ReviewSpecificPullRequestAsync(selectedPr);
                    }
                    else
                    {
                        Console.WriteLine("❌ PR not found.\n");
                    }
                }
                else
                {
                    Console.WriteLine("Cancelled.\n");
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
            Console.WriteLine($"👤 Author: {pr.User.Login}");
            Console.WriteLine($"🌿 Branch: {pr.Head.Ref} → {pr.Base.Ref}");

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

            Console.WriteLine("🎫 Jira update: [Not implemented yet]");
            Console.WriteLine("💬 PR comment: [Not implemented yet]");
            Console.WriteLine();
        }

        /// <summary>
        /// Lists recent commits
        /// </summary>
        public async Task ListRecentCommitsAsync()
        {
            try
            {
                Console.WriteLine("📝 Recent commits:");

                var commits = await _gitHubService.GetCommitsAsync();
                var recentCommits = commits.Take(5);

                foreach (var commit in recentCommits)
                {
                    Console.WriteLine($"  {commit.Sha[..8]} - {commit.Commit.Message} ({commit.Commit.Author.Name})");
                }
                Console.WriteLine();
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

                var pullRequests = await _gitHubService.GetOpenPullRequestsAsync();

                if (pullRequests.Any())
                {
                    foreach (var pr in pullRequests)
                    {
                        Console.WriteLine($"  PR #{pr.Number}: {pr.Title} ({pr.User.Login})");
                    }
                }
                else
                {
                    Console.WriteLine("  No open Pull Requests");
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching PRs: {ex.Message}\n");
            }
        }
    }
}
