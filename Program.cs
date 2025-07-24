using DotNetEnv;
using AICodeReviewer.Services;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Plugins;
using Microsoft.SemanticKernel;

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
            // Add controllers
            services.AddControllers();

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
                          .AllowCredentials();
                });
            });

            // Configuration service (singleton)
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // HTTP client (singleton for performance)
            services.AddSingleton<HttpClient>();

            // Language detection and prompt management services
            services.AddSingleton<ILanguageDetectionService, LanguageDetectionService>();
            services.AddSingleton<IPromptManagementService, PromptManagementService>();

            // Repository management service
            services.AddSingleton<IRepositoryManagementService>(provider =>
            {
                var configService = provider.GetRequiredService<IConfigurationService>();
                var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                    ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
                return new RepositoryManagementService(configService.Settings, token);
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

                return new GitHubService(token, owner, name, configService);
            });

            services.AddSingleton<ICodeReviewService, CodeReviewService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IJiraService, JiraService>();

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

            // Register plugin instances with proper dependencies
            services.AddSingleton<CodeReviewPlugin>(provider =>
                new CodeReviewPlugin(
                    provider.GetRequiredService<ICodeReviewService>(),
                    provider.GetRequiredService<IGitHubService>()));

            services.AddSingleton<GitHubPlugin>(provider =>
                new GitHubPlugin(provider.GetRequiredService<IGitHubService>()));

            services.AddSingleton<TeamsPlugin>();
            services.AddSingleton<JiraPlugin>();

            // Add plugins to kernel after the services are registered
            services.AddSingleton<Kernel>(provider =>
            {
                var kernel = provider.GetRequiredService<Kernel>();
                
                // Add plugins from instances
                kernel.Plugins.AddFromObject(provider.GetRequiredService<CodeReviewPlugin>(), "CodeReview");
                kernel.Plugins.AddFromObject(provider.GetRequiredService<GitHubPlugin>(), "GitHub");
                kernel.Plugins.AddFromObject(provider.GetRequiredService<TeamsPlugin>(), "Teams");
                kernel.Plugins.AddFromObject(provider.GetRequiredService<JiraPlugin>(), "Jira");

                return kernel;
            });

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

            app.UseHttpsRedirection();

            app.UseCors("AllowReactApp");

            app.UseAuthorization();

            app.MapControllers();

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
