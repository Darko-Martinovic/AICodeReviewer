using System;
using System.Threading.Tasks;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AICodeReviewer.Services;
using AICodeReviewer.Application;

namespace AICodeReviewer
{
    /// <summary>
    /// Main entry point for the AI Code Reviewer application
    /// </summary>
    class Program
    {
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

                // Build service provider
                var serviceProvider = BuildServiceProvider();

                // Run the application
                using (serviceProvider)
                {
                    var application = serviceProvider.GetRequiredService<CodeReviewApplication>();
                    await InitializeServicesAsync(serviceProvider);
                    await ShowMainMenuAsync(application);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Application startup failed: {ex.Message}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Builds and configures the dependency injection service provider
        /// </summary>
        private static ServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Configures all services for dependency injection
        /// </summary>
        private static void ConfigureServices(IServiceCollection services)
        {
            // Configuration service (singleton)
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // HTTP client (singleton for performance)
            services.AddSingleton<HttpClient>();

            // Core services (singletons for this console app)
            services.AddSingleton<IAzureOpenAIService>(provider =>
            {
                var httpClient = provider.GetRequiredService<HttpClient>();
                var configService = provider.GetRequiredService<IConfigurationService>();
                
                var endpoint = Environment.GetEnvironmentVariable("AOAI_ENDPOINT")
                    ?? throw new InvalidOperationException("AOAI_ENDPOINT not set");
                var apiKey = Environment.GetEnvironmentVariable("AOAI_APIKEY")
                    ?? throw new InvalidOperationException("AOAI_APIKEY not set");
                var deployment = Environment.GetEnvironmentVariable("CHATCOMPLETION_DEPLOYMENTNAME")
                    ?? throw new InvalidOperationException("CHATCOMPLETION_DEPLOYMENTNAME not set");

                return new AzureOpenAIService(httpClient, endpoint, apiKey, deployment, configService);
            });

            services.AddSingleton<IGitHubService>(provider =>
            {
                var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                    ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
                var owner = Environment.GetEnvironmentVariable("GITHUB_REPO_OWNER")
                    ?? throw new InvalidOperationException("GITHUB_REPO_OWNER not set");
                var name = Environment.GetEnvironmentVariable("GITHUB_REPO_NAME")
                    ?? throw new InvalidOperationException("GITHUB_REPO_NAME not set");

                return new GitHubService(token, owner, name);
            });

            services.AddSingleton<ICodeReviewService, CodeReviewService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IJiraService, JiraService>();

            // Application layer
            services.AddSingleton<CodeReviewApplication>();
        }

        /// <summary>
        /// Initializes services that require async setup
        /// </summary>
        private static async Task InitializeServicesAsync(ServiceProvider serviceProvider)
        {
            // Display configuration summary
            var configService = serviceProvider.GetRequiredService<IConfigurationService>();
            configService.DisplayConfigurationSummary();

            // Initialize GitHub connection
            var gitHubService = serviceProvider.GetRequiredService<IGitHubService>();
            await gitHubService.InitializeAsync();

            Console.WriteLine("✅ Azure OpenAI configured");
            Console.WriteLine("✅ All services initialized successfully");
            Console.WriteLine("🚀 Ready to review code!\n");
        }

        /// <summary>
        /// Shows the main application menu and handles user interaction
        /// </summary>
        private static async Task ShowMainMenuAsync(CodeReviewApplication application)
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
                        await application.ReviewLatestCommitAsync();
                        break;
                    case "2":
                        await application.ReviewPullRequestAsync();
                        break;
                    case "3":
                        await application.ListRecentCommitsAsync();
                        break;
                    case "4":
                        await application.ListOpenPullRequestsAsync();
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
