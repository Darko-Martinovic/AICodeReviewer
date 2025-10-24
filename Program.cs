using DotNetEnv;
using AICodeReviewer.Services;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Plugins;
using AICodeReviewer.Hubs;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace AICodeReviewer
{
    /// <summary>
    /// Web API entry point for the AI Code Reviewer application
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Load environment variables
            Env.Load();

            var builder = WebApplication.CreateBuilder(args);

            // Configure services
            ConfigureServices(builder.Services);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app);

            app.Run();
        }

        /// <summary>
        /// Configures all services for dependency injection
        /// </summary>
        private static void ConfigureServices(IServiceCollection services)
        {
            // Add controllers with JSON configuration
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                });

            // Add API Explorer for Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "AI Code Reviewer API",
                    Version = "v1",
                    Description = "AI-powered code review tool for GitHub repositories using Azure OpenAI"
                });
            });

            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:3000", "https://localhost:3000",
                        "http://localhost:5173", "https://localhost:5173",
                        "http://localhost:5174", "https://localhost:5174",
                        "http://localhost:5175", "https://localhost:5175")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // Required for SignalR
                });
            });

            // Configure HTTPS redirection and HSTS
            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                options.HttpsPort = 7001;
            });

            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            // Configuration service (singleton)
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // HTTP client (singleton for performance)
            services.AddSingleton<HttpClient>();

            // Language detection and prompt management services
            services.AddSingleton<ILanguageDetectionService, LanguageDetectionService>();
            services.AddSingleton<IPromptManagementService, PromptManagementService>();

            // Repository filter service
            services.AddSingleton<IRepositoryFilterService>(provider =>
            {
                var configService = provider.GetRequiredService<IConfigurationService>();
                return new RepositoryFilterService(configService.Settings, configService);
            });

            // GitHub App service (optional)
            services.AddSingleton<IGitHubAppService>(provider =>
            {
                var configService = provider.GetRequiredService<IConfigurationService>();
                var logger = provider.GetService<ILogger<GitHubAppService>>();
                return new GitHubAppService(configService.Settings.GitHub.GitHubApp, configService, logger);
            });

            // Repository management service
            services.AddSingleton<IRepositoryManagementService>(provider =>
            {
                var configService = provider.GetRequiredService<IConfigurationService>();
                var filterService = provider.GetRequiredService<IRepositoryFilterService>();
                var gitHubAppService = provider.GetRequiredService<IGitHubAppService>();
                var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                    ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
                return new RepositoryManagementService(configService.Settings, token, filterService, gitHubAppService);
            });

            // Core services (singletons for this web app)
            services.AddSingleton<IAzureOpenAIService>(provider =>
            {
                var httpClient = provider.GetRequiredService<HttpClient>();
                var configService = provider.GetRequiredService<IConfigurationService>();
                var promptManagementService = provider.GetRequiredService<IPromptManagementService>();

                var endpoint = Environment.GetEnvironmentVariable("AOAI_ENDPOINT")
                    ?? throw new InvalidOperationException("AOAI_ENDPOINT not set");
                var apiKey = Environment.GetEnvironmentVariable("AOAI_APIKEY")
                    ?? throw new InvalidOperationException("AOAI_APIKEY not set");
                var deployment = Environment.GetEnvironmentVariable("CHATCOMPLETION_DEPLOYMENTNAME")
                    ?? throw new InvalidOperationException("CHATCOMPLETION_DEPLOYMENTNAME not set");

                return new AzureOpenAIService(httpClient, endpoint, apiKey, deployment, configService, promptManagementService);
            });

            services.AddSingleton<IGitHubService>(provider =>
            {
                var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                    ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
                var owner = Environment.GetEnvironmentVariable("GITHUB_REPO_OWNER")
                    ?? throw new InvalidOperationException("GITHUB_REPO_OWNER not set");
                var name = Environment.GetEnvironmentVariable("GITHUB_REPO_NAME")
                    ?? throw new InvalidOperationException("GITHUB_REPO_NAME not set");
                var configService = provider.GetRequiredService<IConfigurationService>();
                var gitHubAppService = provider.GetRequiredService<IGitHubAppService>();

                return new GitHubService(token, owner, name, configService, gitHubAppService);
            });

            services.AddSingleton<ICodeReviewService, CodeReviewService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IJiraService, JiraService>();
            services.AddSingleton<ICacheService, CacheService>();

            // Collaboration service for real-time code reviews
            services.AddSingleton<ICollaborationService, CollaborationService>();

            // Add SignalR for real-time communication
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            });

            // Configure Semantic Kernel
            ConfigureSemanticKernel(services);
        }

        /// <summary>
        /// Configures Semantic Kernel with plugins and workflow engine
        /// </summary>
        private static void ConfigureSemanticKernel(IServiceCollection services)
        {
            // Configure Semantic Kernel
            var kernelBuilder = services.AddKernel();

            // Add Azure OpenAI chat completion
            var endpoint = Environment.GetEnvironmentVariable("AOAI_ENDPOINT")
                ?? throw new InvalidOperationException("AOAI_ENDPOINT not set");
            var apiKey = Environment.GetEnvironmentVariable("AOAI_APIKEY")
                ?? throw new InvalidOperationException("AOAI_APIKEY not set");
            var deployment = Environment.GetEnvironmentVariable("CHATCOMPLETION_DEPLOYMENTNAME")
                ?? throw new InvalidOperationException("CHATCOMPLETION_DEPLOYMENTNAME not set");

            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: deployment,
                endpoint: endpoint,
                apiKey: apiKey);

            // Add plugins using AddFromType method
            kernelBuilder.Plugins.AddFromType<CodeReviewPlugin>("CodeReview");
            kernelBuilder.Plugins.AddFromType<GitHubPlugin>("GitHub");
            kernelBuilder.Plugins.AddFromType<TeamsPlugin>("Teams");
            kernelBuilder.Plugins.AddFromType<SlackPlugin>("Slack"); // Now properly configured with error handling

            Console.WriteLine("🔌 Semantic Kernel plugins configured for registration");

            // Register plugins as singleton services for dependency injection
            services.AddSingleton<JiraPlugin>();
            services.AddSingleton<SlackPlugin>();

            // Register workflow engine service
            services.AddSingleton<IWorkflowEngineService, WorkflowEngineService>();
        }

        /// <summary>
        /// Configures the HTTP request pipeline
        /// </summary>
        private static void ConfigurePipeline(WebApplication app)
        {
            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Code Reviewer API v1");
                    c.RoutePrefix = "api/docs"; // Swagger will be available at /api/docs
                });
            }
            else
            {
                // Enable HSTS for production
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowReactApp");

            app.UseAuthorization();

            app.MapControllers();

            // Map SignalR hub for real-time collaboration
            app.MapHub<CollaborationHub>("/collaborationHub");

            // Add a simple health check endpoint
            app.MapGet("/api/health", () => new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            });

            // Root endpoint with API information
            app.MapGet("/", () => new
            {
                Message = "AI Code Reviewer API",
                Documentation = "/api/docs",
                Health = "/api/health"
            });
        }
    }
}
