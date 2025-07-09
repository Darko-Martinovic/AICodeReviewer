using AICodeReviewer.Models.Configuration;
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
            _appSettings = LoadConfiguration();
        }

        /// <summary>
        /// Gets the loaded application settings
        /// </summary>
        public AppSettings Settings => _appSettings;

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
        /// Loads configuration from appsettings.json file
        /// </summary>
        private static void LoadFromJsonFile(AppSettings settings)
        {
            const string configFile = "appsettings.json";

            if (!File.Exists(configFile))
            {
                Console.WriteLine(
                    $"⚠️  Configuration file '{configFile}' not found, using defaults"
                );
                return;
            }

            try
            {
                var jsonContent = File.ReadAllText(configFile);
                var jsonSettings = JsonSerializer.Deserialize<AppSettings>(
                    jsonContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    }
                );

                if (jsonSettings != null)
                {
                    // Copy values from JSON to our settings object
                    CopySettings(jsonSettings, settings);
                    Console.WriteLine($"✅ Configuration loaded from '{configFile}'");
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

            Console.WriteLine("✅ Environment variable overrides applied");
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
