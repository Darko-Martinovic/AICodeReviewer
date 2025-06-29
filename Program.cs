using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using DotNetEnv;
using Octokit;

namespace AICodeReviewer
{
    class Program
    {
        private static GitHubClient gitHubClient;
        private static string repoOwner;
        private static string repoName;

        static async Task Main(string[] args)
        {
            // Load environment variables
            Env.Load();

            Console.WriteLine("=== AI Code Reviewer ===\n");

            // Initialize GitHub client
            await InitializeGitHubClient();

            // Show menu
            await ShowMainMenu();
        }

        static async Task InitializeGitHubClient()
        {
            string githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                ?? throw new InvalidOperationException("GITHUB_TOKEN not set");

            repoOwner = Environment.GetEnvironmentVariable("GITHUB_REPO_OWNER")
                ?? throw new InvalidOperationException("GITHUB_REPO_OWNER not set");

            repoName = Environment.GetEnvironmentVariable("GITHUB_REPO_NAME")
                ?? throw new InvalidOperationException("GITHUB_REPO_NAME not set");

            // Initialize GitHub client
            gitHubClient = new GitHubClient(new ProductHeaderValue("AICodeReviewer"));
            gitHubClient.Credentials = new Credentials(githubToken);

            // Test connection
            try
            {
                var user = await gitHubClient.User.Current();
                Console.WriteLine($"✅ Connected to GitHub as: {user.Login}");

                var repo = await gitHubClient.Repository.Get(repoOwner, repoName);
                Console.WriteLine($"✅ Repository access: {repo.FullName}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GitHub connection failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static async Task ShowMainMenu()
        {
            while (true)
            {
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Review latest commit (Push Event)");
                Console.WriteLine("2. Review Pull Request");
                Console.WriteLine("3. List recent commits");
                Console.WriteLine("4. List open Pull Requests");
                Console.WriteLine("5. Exit");
                Console.Write("\nEnter your choice (1-5): ");

                string choice = Console.ReadLine();
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        await ReviewLatestCommit();
                        break;
                    case "2":
                        await ReviewPullRequest();
                        break;
                    case "3":
                        await ListRecentCommits();
                        break;
                    case "4":
                        await ListOpenPullRequests();
                        break;
                    case "5":
                        Console.WriteLine("Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.\n");
                        break;
                }
            }
        }

        static async Task ReviewLatestCommit()
        {
            try
            {
                Console.WriteLine("🔍 Fetching latest commit...");

                // Get latest commit from main branch
                var commits = await gitHubClient.Repository.Commit.GetAll(repoOwner, repoName, new CommitRequest
                {
                    Sha = "main"
                });

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
                var commitDetail = await gitHubClient.Repository.Commit.Get(repoOwner, repoName, latestCommit.Sha);

                Console.WriteLine($"\n📁 Files changed: {commitDetail.Files.Count}");
                foreach (var file in commitDetail.Files)
                {
                    Console.WriteLine($"  - {file.Status}: {file.Filename} (+{file.Additions}/-{file.Deletions})");
                }

                // TODO: Send to AI for review
                Console.WriteLine("\n🤖 AI Code Review: [Not implemented yet]");
                Console.WriteLine("📤 Teams notification: [Not implemented yet]");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reviewing commit: {ex.Message}\n");
            }
        }

        static async Task ReviewPullRequest()
        {
            try
            {
                Console.WriteLine("🔍 Fetching open Pull Requests...");

                var pullRequests = await gitHubClient.PullRequest.GetAllForRepository(repoOwner, repoName,
                    new PullRequestRequest { State = ItemStateFilter.Open });

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
                        await ReviewSpecificPullRequest(selectedPr);
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

        static async Task ReviewSpecificPullRequest(PullRequest pr)
        {
            Console.WriteLine($"\n📋 Reviewing PR #{pr.Number}: {pr.Title}");
            Console.WriteLine($"👤 Author: {pr.User.Login}");
            Console.WriteLine($"🌿 Branch: {pr.Head.Ref} → {pr.Base.Ref}");

            // Get PR files
            var prFiles = await gitHubClient.PullRequest.Files(repoOwner, repoName, pr.Number);

            Console.WriteLine($"\n📁 Files changed: {prFiles.Count}");
            foreach (var file in prFiles)
            {
                Console.WriteLine($"  - {file.Status}: {file.Filename} (+{file.Additions}/-{file.Deletions})");
            }

            // TODO: Send to AI for review
            Console.WriteLine("\n🤖 AI Code Review: [Not implemented yet]");
            Console.WriteLine("🎫 Jira update: [Not implemented yet]");
            Console.WriteLine("💬 PR comment: [Not implemented yet]");
            Console.WriteLine();
        }

        static async Task ListRecentCommits()
        {
            try
            {
                Console.WriteLine("📝 Recent commits:");

                var commits = await gitHubClient.Repository.Commit.GetAll(repoOwner, repoName, new CommitRequest
                {
                    Sha = "main"
                });

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

        static async Task ListOpenPullRequests()
        {
            try
            {
                Console.WriteLine("🔀 Open Pull Requests:");

                var pullRequests = await gitHubClient.PullRequest.GetAllForRepository(repoOwner, repoName,
                    new PullRequestRequest { State = ItemStateFilter.Open });

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