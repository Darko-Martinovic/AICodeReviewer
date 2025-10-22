using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Services.Interfaces;
using System.Text.RegularExpressions;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for filtering repositories based on configured patterns
    /// </summary>
    public class RepositoryFilterService : IRepositoryFilterService
    {
        private readonly AppSettings _settings;
        private readonly IConfigurationService _configurationService;

        public RepositoryFilterService(AppSettings settings, IConfigurationService configurationService)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        /// <summary>
        /// Filters a list of repositories based on the configured filter settings
        /// </summary>
        public List<RepositoryInfo> FilterRepositories(List<RepositoryInfo> repositories)
        {
            var filterSettings = _settings.GitHub.RepositoryFilter;

            // If filtering is disabled, return all repositories
            if (!filterSettings.EnableFiltering)
            {
                return repositories;
            }

            // If no patterns are configured, use default mode
            if (!filterSettings.IncludePatterns.Any() && !filterSettings.ExcludePatterns.Any())
            {
                return filterSettings.DefaultMode == FilterMode.ShowAll ? repositories : new List<RepositoryInfo>();
            }

            var filteredRepos = new List<RepositoryInfo>();

            foreach (var repo in repositories)
            {
                var shouldInclude = ShouldIncludeRepository(repo, filterSettings);
                if (shouldInclude)
                {
                    filteredRepos.Add(repo);
                }
            }

            return filteredRepos;
        }

        /// <summary>
        /// Determines if a repository should be included based on filter patterns
        /// </summary>
        private bool ShouldIncludeRepository(RepositoryInfo repository, RepositoryFilterSettings filterSettings)
        {
            // First check exclude patterns - if any match, exclude the repository
            foreach (var excludePattern in filterSettings.ExcludePatterns)
            {
                if (MatchesPattern(repository, excludePattern))
                {
                    return false; // Exclude this repository
                }
            }

            // If there are include patterns, the repository must match at least one
            if (filterSettings.IncludePatterns.Any())
            {
                foreach (var includePattern in filterSettings.IncludePatterns)
                {
                    if (MatchesPattern(repository, includePattern))
                    {
                        return true; // Include this repository
                    }
                }
                return false; // No include patterns matched
            }

            // No include patterns specified and not excluded, so include by default
            return true;
        }

        /// <summary>
        /// Checks if a repository matches a specific filter pattern
        /// </summary>
        private bool MatchesPattern(RepositoryInfo repository, RepositoryFilterPattern pattern)
        {
            // Check provider filter if specified
            if (!string.IsNullOrEmpty(pattern.Provider))
            {
                // For now, we only support GitHub, but this can be extended
                var repoProvider = GetRepositoryProvider(repository);
                if (!string.Equals(repoProvider, pattern.Provider, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Check owner filter if specified
            if (!string.IsNullOrEmpty(pattern.Owner))
            {
                var comparison = pattern.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (!string.Equals(repository.Owner, pattern.Owner, comparison))
                {
                    return false;
                }
            }

            // Check name pattern
            return MatchesWildcardPattern(repository.Name, pattern.Pattern, pattern.CaseSensitive);
        }

        /// <summary>
        /// Matches a string against a wildcard pattern (supports * as wildcard)
        /// </summary>
        private bool MatchesWildcardPattern(string input, string pattern, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return true; // Empty pattern matches everything
            }

            // Convert wildcard pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            return Regex.IsMatch(input, regexPattern, regexOptions);
        }

        /// <summary>
        /// Determines the Git hosting provider for a repository
        /// </summary>
        private string GetRepositoryProvider(RepositoryInfo repository)
        {
            // For now, assume all repositories are from GitHub
            // This can be extended to detect provider from URL or other properties
            return "github";
        }

        /// <summary>
        /// Gets the current repository filter settings
        /// </summary>
        public RepositoryFilterSettings GetFilterSettings()
        {
            return _settings.GitHub.RepositoryFilter;
        }

        /// <summary>
        /// Updates the repository filter settings
        /// </summary>
        public async Task UpdateFilterSettingsAsync(RepositoryFilterSettings newSettings)
        {
            _settings.GitHub.RepositoryFilter = newSettings ?? throw new ArgumentNullException(nameof(newSettings));

            // Persist the settings to appsettings.json
            try
            {
                await _configurationService.SaveSettingsAsync();
                Console.WriteLine("✅ Repository filter settings saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Failed to save repository filter settings: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates a repository filter pattern
        /// </summary>
        public (bool IsValid, string? ErrorMessage) ValidatePattern(RepositoryFilterPattern pattern)
        {
            if (pattern == null)
            {
                return (false, "Pattern cannot be null");
            }

            if (string.IsNullOrWhiteSpace(pattern.Pattern))
            {
                return (false, "Pattern cannot be empty");
            }

            // Check if pattern contains valid wildcard syntax
            try
            {
                var regexPattern = "^" + Regex.Escape(pattern.Pattern).Replace("\\*", ".*") + "$";
                new Regex(regexPattern);
            }
            catch (ArgumentException ex)
            {
                return (false, $"Invalid pattern syntax: {ex.Message}");
            }

            // Validate provider if specified
            if (!string.IsNullOrEmpty(pattern.Provider))
            {
                var supportedProviders = new[] { "github", "gitlab", "bitbucket", "azure-devops" };
                if (!supportedProviders.Contains(pattern.Provider.ToLowerInvariant()))
                {
                    return (false, $"Unsupported provider: {pattern.Provider}. Supported providers: {string.Join(", ", supportedProviders)}");
                }
            }

            return (true, null);
        }

        /// <summary>
        /// Tests a pattern against a repository name to see if it would match
        /// </summary>
        public bool TestPattern(string repositoryName, string owner, RepositoryFilterPattern pattern)
        {
            var testRepo = new RepositoryInfo
            {
                Name = repositoryName,
                Owner = owner
            };

            return MatchesPattern(testRepo, pattern);
        }
    }
}