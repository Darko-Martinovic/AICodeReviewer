using System;
using System.Threading.Tasks;
using DotNetEnv;
using AICodeReviewer.Services;
using AICodeReviewer.Application;

namespace AICodeReviewer
{
    /// <summary>
    /// Main entry point for the AI Code Reviewer application
    /// </summary>
    class Program
    {
        private static CodeReviewApplication _application = null!;

        static async Task Main(string[] args)
        {
            try
            {
                // Load environment variables
                Console.WriteLine("🔍 Debug: Loading environment variables from .env file...");
                Env.Load();
                Console.WriteLine("🔍 Debug: Environment variables loaded");

                Console.WriteLine("🤖 AI Code Reviewer");
                Console.WriteLine("Starting up...");
                Console.WriteLine();

                // Initialize services
                await InitializeServicesAsync();

                // Show menu
                await ShowMainMenuAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Application startup failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Initializes all required services and dependencies
        /// </summary>
        static async Task InitializeServicesAsync()
        {
            // Initialize configuration service first
            var configurationService = new ConfigurationService();
            configurationService.DisplayConfigurationSummary();

            // Get configuration from environment variables
            string githubToken =
                Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                ?? throw new InvalidOperationException("GITHUB_TOKEN not set");

            string repoOwner =
                Environment.GetEnvironmentVariable("GITHUB_REPO_OWNER")
                ?? throw new InvalidOperationException("GITHUB_REPO_OWNER not set");

            string repoName =
                Environment.GetEnvironmentVariable("GITHUB_REPO_NAME")
                ?? throw new InvalidOperationException("GITHUB_REPO_NAME not set");

            string aoaiEndpoint =
                Environment.GetEnvironmentVariable("AOAI_ENDPOINT")
                ?? throw new InvalidOperationException("AOAI_ENDPOINT not set");

            string aoaiApiKey =
                Environment.GetEnvironmentVariable("AOAI_APIKEY")
                ?? throw new InvalidOperationException("AOAI_APIKEY not set");

            string chatDeployment =
                Environment.GetEnvironmentVariable("CHATCOMPLETION_DEPLOYMENTNAME")
                ?? throw new InvalidOperationException("CHATCOMPLETION_DEPLOYMENTNAME not set");

            // Initialize services with configuration injection
            var httpClient = new System.Net.Http.HttpClient();
            var azureOpenAIService = new AzureOpenAIService(
                httpClient,
                aoaiEndpoint,
                aoaiApiKey,
                chatDeployment,
                configurationService
            );
            var gitHubService = new GitHubService(githubToken, repoOwner, repoName);
            var codeReviewService = new CodeReviewService(
                azureOpenAIService,
                gitHubService,
                configurationService
            );
            var notificationService = new NotificationService(configurationService);
            var jiraService = new JiraService();

            // Initialize GitHub connection
            await gitHubService.InitializeAsync();

            // Initialize application
            _application = new CodeReviewApplication(
                gitHubService,
                codeReviewService,
                notificationService,
                jiraService
            );

            Console.WriteLine("✅ Azure OpenAI configured");
            Console.WriteLine("✅ All services initialized successfully");
            Console.WriteLine("🚀 Ready to review code!\n");
        }

        /// <summary>
        /// Shows the main application menu and handles user interaction
        /// </summary>
        static async Task ShowMainMenuAsync()
        {
            while (true)
            {
                Console.WriteLine("🤖 AI Code Reviewer");
                Console.WriteLine();
                Console.WriteLine("Choose an option:");
                Console.WriteLine();
                Console.WriteLine("  1. 🔍 Review latest commit (Push Event)");
                Console.WriteLine("  2. 🔀 Review Pull Request");
                Console.WriteLine("  3. 📝 List recent commits");
                Console.WriteLine("  4. 📋 List open Pull Requests");
                Console.WriteLine("  5. 🚪 Exit");
                Console.WriteLine();
                Console.Write("Enter your choice (1-5): ");

                string? choice = Console.ReadLine();
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        await _application.ReviewLatestCommitAsync();
                        break;
                    case "2":
                        await _application.ReviewPullRequestAsync();
                        break;
                    case "3":
                        await _application.ListRecentCommitsAsync();
                        break;
                    case "4":
                        await _application.ListOpenPullRequestsAsync();
                        break;
                    case "5":
                        Console.WriteLine("👋 Goodbye! Thanks for using AI Code Reviewer!");
                        return;
                    default:
                        Console.WriteLine("❌ Invalid choice. Please try again.\n");
                        break;
                }
            }
        }
    }
}
