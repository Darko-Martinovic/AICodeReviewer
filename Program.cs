using AICodeReviewer.Services;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Plugins;
using AICodeReviewer.Hubs;
using Microsoft.SemanticKernel;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

namespace AICodeReviewer
{
    /// <summary>
    /// Web API entry point for the AI Code Reviewer application
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure services
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app);

            app.Run();
        }

        /// <summary>
        /// Configures all services for dependency injection
        /// </summary>
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add API versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader()
                );
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // Add controllers with JSON configuration
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                });

            // Add API Explorer for Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Add CORS - Allow all origins for development (SIMPLE VERSION)
            services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    Console.WriteLine("🔧 CORS: Configured to allow all origins");
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                    // Note: Cannot use AllowCredentials() with AllowAnyOrigin()
                });
            });

            // Configure HTTPS redirection and HSTS - DISABLED for HTTP development
            // services.AddHttpsRedirection(options =>
            // {
            //     options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            //     options.HttpsPort = 7001;
            // });

            // services.AddHsts(options =>
            // {
            //     options.Preload = true;
            //     options.IncludeSubDomains = true;
            //     options.MaxAge = TimeSpan.FromDays(365);
            // });

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
                var configuration = provider.GetRequiredService<IConfiguration>();
                var token = configuration["GitHub:Token"]
                    ?? throw new InvalidOperationException("GitHub:Token not set in configuration");
                return new RepositoryManagementService(configService.Settings, token, filterService, gitHubAppService);
            });

            // Core services (singletons for this web app)
            services.AddSingleton<IAzureOpenAIService>(provider =>
            {
                var httpClient = provider.GetRequiredService<HttpClient>();
                var configService = provider.GetRequiredService<IConfigurationService>();
                var promptManagementService = provider.GetRequiredService<IPromptManagementService>();
                var configuration = provider.GetRequiredService<IConfiguration>();

                var endpoint = configuration["AzureOpenAI:Endpoint"]
                    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not set in configuration");
                var apiKey = configuration["AzureOpenAI:ApiKey"]
                    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not set in configuration");
                var deployment = configuration["AzureOpenAI:DeploymentName"]
                    ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not set in configuration");

                return new AzureOpenAIService(httpClient, endpoint, apiKey, deployment, configService, promptManagementService);
            });

            services.AddSingleton<IGitHubService>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var token = configuration["GitHub:Token"]
                    ?? throw new InvalidOperationException("GitHub:Token not set in configuration");
                var owner = configuration["GitHub:Owner"]
                    ?? throw new InvalidOperationException("GitHub:Owner not set in configuration");
                var name = configuration["GitHub:Name"]
                    ?? throw new InvalidOperationException("GitHub:Name not set in configuration");
                var configService = provider.GetRequiredService<IConfigurationService>();
                var gitHubAppService = provider.GetRequiredService<IGitHubAppService>();

                return new GitHubService(token, owner, name, configService, gitHubAppService);
            });

            services.AddSingleton<ICodeReviewService, CodeReviewService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IJiraService, JiraService>();
            services.AddSingleton<ICacheService, CacheService>();
            services.AddSingleton<ICodeValidationService, CodeValidationService>();

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
            ConfigureSemanticKernel(services, configuration);
        }

        /// <summary>
        /// Configures Semantic Kernel with plugins and workflow engine
        /// </summary>
        private static void ConfigureSemanticKernel(IServiceCollection services, IConfiguration configuration)
        {
            // Configure Semantic Kernel
            var kernelBuilder = services.AddKernel();

            // Add Azure OpenAI chat completion
            var endpoint = configuration["AzureOpenAI:Endpoint"]
                ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not set in configuration");
            var apiKey = configuration["AzureOpenAI:ApiKey"]
                ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not set in configuration");
            var deployment = configuration["AzureOpenAI:DeploymentName"]
                ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not set in configuration");

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
                var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                    {
                        c.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
                            $"AI Code Reviewer API {description.ApiVersion}");
                    }
                    c.RoutePrefix = "api/docs"; // Swagger will be available at /api/docs
                });
            }
            else
            {
                // Enable HSTS for production - DISABLED for HTTP development
                // app.UseHsts();
            }

            // HTTPS redirection disabled for HTTP development
            // app.UseHttpsRedirection();

            // Handle OPTIONS preflight requests explicitly
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, PATCH");
                    context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
                    context.Response.Headers.Append("Access-Control-Max-Age", "3600");
                    context.Response.StatusCode = 200;
                    await context.Response.CompleteAsync();
                    return;
                }
                await next();
            });

            app.UseCors("AllowReactApp");

            app.UseAuthorization();

            app.MapControllers();

            // Map SignalR hub for real-time collaboration with CORS
            app.MapHub<CollaborationHub>("/collaborationHub")
               .RequireCors("AllowReactApp");

            // Add a simple health check endpoint with CORS (versioned)
            app.MapGet("/api/v1/health", () => new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            }).RequireCors("AllowReactApp");

            // Root endpoint with API information with CORS
            app.MapGet("/", () => new
            {
                Message = "AI Code Reviewer API",
                Documentation = "/api/docs",
                Health = "/api/health"
            }).RequireCors("AllowReactApp");
        }
    }
}
