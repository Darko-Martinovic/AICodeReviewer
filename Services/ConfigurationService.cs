using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Services.Interfaces;
using System.Text.Json;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for loading and managing application configuration from multiple sources
    /// Priority: appsettings.json > Environment Variables > Defaults
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly AppSettings _appSettings;

        public ConfigurationService()
        {
            Console.WriteLine("🔧 LOADING CONFIGURATION - Starting initialization...");
            _appSettings = LoadConfiguration();
            Console.WriteLine($"🔧 CONFIGURATION COMPLETE - Temperature: {_appSettings.AzureOpenAI.Temperature}");

            // Debug repository filter settings
            var filterSettings = _appSettings.GitHub.RepositoryFilter;
            Console.WriteLine($"🔍 DEBUG - Repository Filter Settings:");
            Console.WriteLine($"    EnableFiltering: {filterSettings.EnableFiltering}");
            Console.WriteLine($"    DefaultMode: {filterSettings.DefaultMode}");
            Console.WriteLine($"    IncludePatterns Count: {filterSettings.IncludePatterns.Count}");
            Console.WriteLine($"    ExcludePatterns Count: {filterSettings.ExcludePatterns.Count}");

            if (filterSettings.IncludePatterns.Any())
            {
                foreach (var pattern in filterSettings.IncludePatterns)
                {
                    Console.WriteLine($"    Include Pattern: '{pattern.Pattern}' (Owner: {pattern.Owner}, Provider: {pattern.Provider})");
                }
            }
        }

        /// <summary>
        /// Gets the loaded application settings
        /// </summary>
        public AppSettings Settings => _appSettings;

        /// <summary>
        /// Saves the current settings back to appsettings.json
        /// </summary>
        public async Task SaveSettingsAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null, // Use PascalCase (default C# naming)
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };

                var jsonContent = JsonSerializer.Serialize(_appSettings, options);
                await File.WriteAllTextAsync("appsettings.json", jsonContent);

                if (_appSettings.DebugLogging)
                {
                    Console.WriteLine("✅ Settings saved to appsettings.json");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error saving settings: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads configuration from multiple sources with proper precedence
        /// </summary>
        private static AppSettings LoadConfiguration()
        {
            var settings = new AppSettings();

            // 1. Load from appsettings.json if it exists
            LoadFromJsonFile(settings);

            // 2. Override with environment variables if they exist
            LoadFromEnvironmentVariables(settings);

            return settings;
        }

        /// <summary>
        /// Loads configuration from appsettings.json and environment-specific files
        /// </summary>
        private static void LoadFromJsonFile(AppSettings settings)
        {
            // Get current environment (default to Development if not set)
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Load base configuration first
            const string baseConfigFile = "appsettings.json";
            LoadSingleJsonFile(baseConfigFile, settings);

            // Load environment-specific configuration (overrides base)
            var envConfigFile = $"appsettings.{environment}.json";
            LoadSingleJsonFile(envConfigFile, settings);
        }

        /// <summary>
        /// Loads configuration from a single JSON file
        /// </summary>
        private static void LoadSingleJsonFile(string configFile, AppSettings settings)
        {
            if (!File.Exists(configFile))
            {
                if (configFile == "appsettings.json")
                {
                    Console.WriteLine($"⚠️  Base configuration file '{configFile}' not found, using defaults");
                }
                else
                {
                    Console.WriteLine($"📝 Environment-specific config '{configFile}' not found, using base settings");
                }
                return;
            }

            try
            {
                var jsonContent = File.ReadAllText(configFile);
                Console.WriteLine($"📄 DEBUG - JSON file '{configFile}' content length: {jsonContent.Length}");

                // Check if the JSON contains the expected sections
                Console.WriteLine($"📄 DEBUG - JSON contains 'GitHub': {jsonContent.Contains("\"GitHub\"")}");
                Console.WriteLine($"📄 DEBUG - JSON contains 'RepositoryFilter': {jsonContent.Contains("\"RepositoryFilter\"")}");
                Console.WriteLine($"📄 DEBUG - JSON contains 'EnableFiltering': {jsonContent.Contains("\"EnableFiltering\"")}");
                Console.WriteLine($"📄 DEBUG - JSON contains 'Slack': {jsonContent.Contains("\"Slack\"")}");
                Console.WriteLine($"📄 DEBUG - JSON contains 'WebhookUrl': {jsonContent.Contains("\"WebhookUrl\"")}");

                var jsonSettings = JsonSerializer.Deserialize<AppSettings>(
                    jsonContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                    }
                );

                if (jsonSettings != null)
                {
                    Console.WriteLine($"📄 DEBUG - Deserialized settings: GitHub section is null: {jsonSettings.GitHub == null}");
                    Console.WriteLine($"📄 DEBUG - Deserialized settings: Slack section is null: {jsonSettings.Slack == null}");
                    if (jsonSettings.Slack != null)
                    {
                        Console.WriteLine($"📄 DEBUG - Slack.WebhookUrl: '{jsonSettings.Slack.WebhookUrl}'");
                        Console.WriteLine($"📄 DEBUG - Slack.BotToken: '{jsonSettings.Slack.BotToken}'");
                        Console.WriteLine($"📄 DEBUG - Slack.EnableNotifications: {jsonSettings.Slack.EnableNotifications}");
                    }
                    if (jsonSettings.GitHub != null)
                    {
                        Console.WriteLine($"📄 DEBUG - GitHub.RepositoryFilter is null: {jsonSettings.GitHub.RepositoryFilter == null}");
                        if (jsonSettings.GitHub.RepositoryFilter != null)
                        {
                            Console.WriteLine($"📄 DEBUG - Before CopySettings - EnableFiltering: {jsonSettings.GitHub.RepositoryFilter.EnableFiltering}");
                            Console.WriteLine($"📄 DEBUG - Before CopySettings - IncludePatterns count: {jsonSettings.GitHub.RepositoryFilter.IncludePatterns.Count}");
                        }
                    }

                    // Copy values from JSON to our settings object
                    CopySettings(jsonSettings, settings);
                    if (settings.DebugLogging)
                    {
                        Console.WriteLine($"✅ Configuration loaded from '{configFile}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error loading '{configFile}': {ex.Message}");
                Console.WriteLine("Using default configuration values");
            }
        }

        /// <summary>
        /// Overrides settings with environment variables where they exist
        /// </summary>
        private static void LoadFromEnvironmentVariables(AppSettings settings)
        {
            // Azure OpenAI settings (keep existing environment variable support)
            if (
                float.TryParse(
                    Environment.GetEnvironmentVariable("AI_TEMPERATURE"),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var temp
                )
            )
            {
                settings.AzureOpenAI.Temperature = temp;
            }

            if (
                int.TryParse(Environment.GetEnvironmentVariable("AI_MAX_TOKENS"), out var maxTokens)
            )
            {
                settings.AzureOpenAI.MaxTokens = maxTokens;
            }

            if (
                int.TryParse(
                    Environment.GetEnvironmentVariable("AI_CONTENT_LIMIT"),
                    out var contentLimit
                )
            )
            {
                settings.AzureOpenAI.ContentLimit = contentLimit;
            }

            var systemPrompt = Environment.GetEnvironmentVariable("AI_SYSTEM_PROMPT");
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                settings.AzureOpenAI.SystemPrompt = systemPrompt;
            }

            // Language-specific prompt overrides
            LoadLanguageSpecificPromptsFromEnvironment(settings);

            // Additional environment overrides can be added here
            var apiVersion = Environment.GetEnvironmentVariable("AI_API_VERSION");
            if (!string.IsNullOrEmpty(apiVersion))
            {
                settings.AzureOpenAI.ApiVersion = apiVersion;
            }

            // Code Review settings
            if (int.TryParse(Environment.GetEnvironmentVariable("MAX_FILES_TO_REVIEW"), out var maxFiles))
            {
                settings.CodeReview.MaxFilesToReview = maxFiles;
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("MAX_ISSUES_IN_SUMMARY"), out var maxIssues))
            {
                settings.CodeReview.MaxIssuesInSummary = maxIssues;
            }

            if (settings.DebugLogging)
            {
                Console.WriteLine("✅ Environment variable overrides applied");
            }
        }

        /// <summary>
        /// Loads language-specific prompts from environment variables
        /// </summary>
        private static void LoadLanguageSpecificPromptsFromEnvironment(AppSettings settings)
        {
            // C# prompts
            var csharpSystemPrompt = Environment.GetEnvironmentVariable("AI_CSharp_SYSTEM_PROMPT");
            if (!string.IsNullOrEmpty(csharpSystemPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.CSharp.SystemPrompt = csharpSystemPrompt;
            }

            var csharpUserPrompt = Environment.GetEnvironmentVariable("AI_CSharp_USER_PROMPT");
            if (!string.IsNullOrEmpty(csharpUserPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.CSharp.UserPromptTemplate = csharpUserPrompt;
            }

            // VB.NET prompts
            var vbNetSystemPrompt = Environment.GetEnvironmentVariable("AI_VbNet_SYSTEM_PROMPT");
            if (!string.IsNullOrEmpty(vbNetSystemPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.VbNet.SystemPrompt = vbNetSystemPrompt;
            }

            var vbNetUserPrompt = Environment.GetEnvironmentVariable("AI_VbNet_USER_PROMPT");
            if (!string.IsNullOrEmpty(vbNetUserPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.VbNet.UserPromptTemplate = vbNetUserPrompt;
            }

            // SQL prompts
            var sqlSystemPrompt = Environment.GetEnvironmentVariable("AI_Sql_SYSTEM_PROMPT");
            if (!string.IsNullOrEmpty(sqlSystemPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.Sql.SystemPrompt = sqlSystemPrompt;
            }

            var sqlUserPrompt = Environment.GetEnvironmentVariable("AI_Sql_USER_PROMPT");
            if (!string.IsNullOrEmpty(sqlUserPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.Sql.UserPromptTemplate = sqlUserPrompt;
            }

            // JavaScript prompts
            var jsSystemPrompt = Environment.GetEnvironmentVariable("AI_JavaScript_SYSTEM_PROMPT");
            if (!string.IsNullOrEmpty(jsSystemPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.JavaScript.SystemPrompt = jsSystemPrompt;
            }

            var jsUserPrompt = Environment.GetEnvironmentVariable("AI_JavaScript_USER_PROMPT");
            if (!string.IsNullOrEmpty(jsUserPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.JavaScript.UserPromptTemplate = jsUserPrompt;
            }

            // TypeScript prompts
            var tsSystemPrompt = Environment.GetEnvironmentVariable("AI_TypeScript_SYSTEM_PROMPT");
            if (!string.IsNullOrEmpty(tsSystemPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.TypeScript.SystemPrompt = tsSystemPrompt;
            }

            var tsUserPrompt = Environment.GetEnvironmentVariable("AI_TypeScript_USER_PROMPT");
            if (!string.IsNullOrEmpty(tsUserPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.TypeScript.UserPromptTemplate = tsUserPrompt;
            }

            // React prompts
            var reactSystemPrompt = Environment.GetEnvironmentVariable("AI_React_SYSTEM_PROMPT");
            if (!string.IsNullOrEmpty(reactSystemPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.React.SystemPrompt = reactSystemPrompt;
            }

            var reactUserPrompt = Environment.GetEnvironmentVariable("AI_React_USER_PROMPT");
            if (!string.IsNullOrEmpty(reactUserPrompt))
            {
                settings.AzureOpenAI.LanguagePrompts.React.UserPromptTemplate = reactUserPrompt;
            }
        }

        /// <summary>
        /// Copies settings from source to destination
        /// </summary>
        private static void CopySettings(AppSettings source, AppSettings destination)
        {
            // Azure OpenAI
            if (source.AzureOpenAI != null)
            {
                destination.AzureOpenAI.ApiVersion = source.AzureOpenAI.ApiVersion;
                destination.AzureOpenAI.Temperature = source.AzureOpenAI.Temperature;
                destination.AzureOpenAI.MaxTokens = source.AzureOpenAI.MaxTokens;
                destination.AzureOpenAI.ContentLimit = source.AzureOpenAI.ContentLimit;
                destination.AzureOpenAI.SystemPrompt = source.AzureOpenAI.SystemPrompt;

                // Copy language-specific prompts
                if (source.AzureOpenAI.LanguagePrompts != null)
                {
                    destination.AzureOpenAI.LanguagePrompts = source.AzureOpenAI.LanguagePrompts;
                }
            }

            // Code Review
            if (source.CodeReview != null)
            {
                destination.CodeReview.MaxFilesToReview = source.CodeReview.MaxFilesToReview;
                destination.CodeReview.MaxIssuesInSummary = source.CodeReview.MaxIssuesInSummary;
            }

            // Teams
            if (source.Teams != null)
            {
                if (source.Teams.SimulationDelays != null)
                {
                    destination.Teams.SimulationDelays = source.Teams.SimulationDelays;
                }
                if (source.Teams.TeamMembers?.Any() == true)
                {
                    destination.Teams.TeamMembers = new List<string>(source.Teams.TeamMembers);
                }
                if (source.Teams.ResponseRange != null)
                {
                    destination.Teams.ResponseRange = source.Teams.ResponseRange;
                }
                if (source.Teams.RandomDelayRange != null)
                {
                    destination.Teams.RandomDelayRange = source.Teams.RandomDelayRange;
                }
            }

            // GitHub
            if (source.GitHub != null)
            {
                destination.GitHub.MaxCommitsToList = source.GitHub.MaxCommitsToList;
                destination.GitHub.MaxPullRequestsToList = source.GitHub.MaxPullRequestsToList;
            }

            // GitHub
            if (source.GitHub != null)
            {
                destination.GitHub.MaxCommitsToList = source.GitHub.MaxCommitsToList;
                destination.GitHub.MaxPullRequestsToList = source.GitHub.MaxPullRequestsToList;

                // Copy GitHub App settings
                if (source.GitHub.GitHubApp != null)
                {
                    destination.GitHub.GitHubApp = new GitHubAppSettings
                    {
                        AppId = source.GitHub.GitHubApp.AppId,
                        ClientId = source.GitHub.GitHubApp.ClientId,
                        InstallationId = source.GitHub.GitHubApp.InstallationId,
                        PrivateKeyPath = source.GitHub.GitHubApp.PrivateKeyPath,
                        UseAppAuthentication = source.GitHub.GitHubApp.UseAppAuthentication
                    };
                }

                // Copy repository filter settings only if they contain non-default values
                if (source.GitHub.RepositoryFilter != null &&
                    (source.GitHub.RepositoryFilter.EnableFiltering ||
                     source.GitHub.RepositoryFilter.IncludePatterns?.Any() == true ||
                     source.GitHub.RepositoryFilter.ExcludePatterns?.Any() == true))
                {
                    destination.GitHub.RepositoryFilter = new RepositoryFilterSettings
                    {
                        EnableFiltering = source.GitHub.RepositoryFilter.EnableFiltering,
                        DefaultMode = source.GitHub.RepositoryFilter.DefaultMode,
                        IncludePatterns = source.GitHub.RepositoryFilter.IncludePatterns?.Select(p => new RepositoryFilterPattern
                        {
                            Pattern = p.Pattern,
                            Provider = p.Provider,
                            Owner = p.Owner,
                            CaseSensitive = p.CaseSensitive,
                            Description = p.Description
                        }).ToList() ?? new List<RepositoryFilterPattern>(),
                        ExcludePatterns = source.GitHub.RepositoryFilter.ExcludePatterns?.Select(p => new RepositoryFilterPattern
                        {
                            Pattern = p.Pattern,
                            Provider = p.Provider,
                            Owner = p.Owner,
                            CaseSensitive = p.CaseSensitive,
                            Description = p.Description
                        }).ToList() ?? new List<RepositoryFilterPattern>()
                    };
                }

                // Copy repository history and default repository
                if (source.GitHub.RepositoryHistory?.Any() == true)
                {
                    destination.GitHub.RepositoryHistory = new List<RepositoryInfo>(source.GitHub.RepositoryHistory);
                }

                if (source.GitHub.DefaultRepository != null)
                {
                    destination.GitHub.DefaultRepository = new RepositoryInfo
                    {
                        Owner = source.GitHub.DefaultRepository.Owner,
                        Name = source.GitHub.DefaultRepository.Name,
                        Description = source.GitHub.DefaultRepository.Description,
                        DefaultBranch = source.GitHub.DefaultRepository.DefaultBranch,
                        IsPrivate = source.GitHub.DefaultRepository.IsPrivate,
                        StarCount = source.GitHub.DefaultRepository.StarCount,
                        ForkCount = source.GitHub.DefaultRepository.ForkCount
                    };
                }
            }

            // Jira
            if (source.Jira != null)
            {
                if (source.Jira.DefaultLabels?.Any() == true)
                {
                    destination.Jira.DefaultLabels = new List<string>(source.Jira.DefaultLabels);
                }
                if (source.Jira.SeverityLabels?.Any() == true)
                {
                    destination.Jira.SeverityLabels = new Dictionary<string, string>(
                        source.Jira.SeverityLabels
                    );
                }
            }

            // Slack
            if (source.Slack != null)
            {
                destination.Slack.WebhookUrl = source.Slack.WebhookUrl ?? "";
                destination.Slack.BotToken = source.Slack.BotToken ?? "";
                destination.Slack.AppToken = source.Slack.AppToken ?? "";
                destination.Slack.AppId = source.Slack.AppId ?? "";
                destination.Slack.WorkspaceId = source.Slack.WorkspaceId ?? "";
                destination.Slack.DefaultChannel = source.Slack.DefaultChannel ?? "#code-reviews";
                destination.Slack.EnableNotifications = source.Slack.EnableNotifications;

                if (source.Slack.MessageOptions != null)
                {
                    destination.Slack.MessageOptions.Username = source.Slack.MessageOptions.Username ?? "AI Code Reviewer";
                    destination.Slack.MessageOptions.IconEmoji = source.Slack.MessageOptions.IconEmoji ?? ":robot_face:";
                    destination.Slack.MessageOptions.ThreadReplies = source.Slack.MessageOptions.ThreadReplies;
                }

                if (source.Slack.ErrorHandling != null)
                {
                    destination.Slack.ErrorHandling.RetryAttempts = source.Slack.ErrorHandling.RetryAttempts;
                    destination.Slack.ErrorHandling.RetryDelayMs = source.Slack.ErrorHandling.RetryDelayMs;
                    destination.Slack.ErrorHandling.FailSilently = source.Slack.ErrorHandling.FailSilently;
                    destination.Slack.ErrorHandling.LogErrors = source.Slack.ErrorHandling.LogErrors;
                }
            }
        }

        /// <summary>
        /// Displays current configuration summary
        /// </summary>
        public void DisplayConfigurationSummary()
        {
            Console.WriteLine("📋 Configuration Summary:");
            Console.WriteLine($"  🤖 AI Temperature: {_appSettings.AzureOpenAI.Temperature}");
            Console.WriteLine($"  🔢 Max Tokens: {_appSettings.AzureOpenAI.MaxTokens}");
            Console.WriteLine($"  📏 Content Limit: {_appSettings.AzureOpenAI.ContentLimit}");
            Console.WriteLine(
                $"  📝 Max Files to Review: {_appSettings.CodeReview.MaxFilesToReview}"
            );
            Console.WriteLine($"  👥 Team Members: {_appSettings.Teams.TeamMembers.Count}");
            Console.WriteLine($"  🔗 API Version: {_appSettings.AzureOpenAI.ApiVersion}");

            // Display language-specific prompt information
            Console.WriteLine("  🌐 Language-Specific Prompts:");
            var supportedLanguages = new[] { "CSharp", "VbNet", "Sql", "JavaScript", "TypeScript", "React" };
            foreach (var language in supportedLanguages)
            {
                var hasCustomPrompt = language switch
                {
                    "CSharp" => !string.IsNullOrEmpty(_appSettings.AzureOpenAI.LanguagePrompts.CSharp.SystemPrompt),
                    "VbNet" => !string.IsNullOrEmpty(_appSettings.AzureOpenAI.LanguagePrompts.VbNet.SystemPrompt),
                    "Sql" => !string.IsNullOrEmpty(_appSettings.AzureOpenAI.LanguagePrompts.Sql.SystemPrompt),
                    "JavaScript" => !string.IsNullOrEmpty(_appSettings.AzureOpenAI.LanguagePrompts.JavaScript.SystemPrompt),
                    "TypeScript" => !string.IsNullOrEmpty(_appSettings.AzureOpenAI.LanguagePrompts.TypeScript.SystemPrompt),
                    "React" => !string.IsNullOrEmpty(_appSettings.AzureOpenAI.LanguagePrompts.React.SystemPrompt),
                    _ => false
                };

                Console.WriteLine($"    {language}: {(hasCustomPrompt ? "✅ Custom" : "⚠️  Default")}");
            }
        }
    }
}
