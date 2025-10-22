using AICodeReviewer.Models.Configuration;

namespace AICodeReviewer.Services.Interfaces
{
    /// <summary>
    /// Interface for repository filtering service
    /// </summary>
    public interface IRepositoryFilterService
    {
        /// <summary>
        /// Filters a list of repositories based on the configured filter settings
        /// </summary>
        List<RepositoryInfo> FilterRepositories(List<RepositoryInfo> repositories);

        /// <summary>
        /// Gets the current repository filter settings
        /// </summary>
        RepositoryFilterSettings GetFilterSettings();

        /// <summary>
        /// Updates the repository filter settings
        /// </summary>
        Task UpdateFilterSettingsAsync(RepositoryFilterSettings newSettings);

        /// <summary>
        /// Validates a repository filter pattern
        /// </summary>
        (bool IsValid, string? ErrorMessage) ValidatePattern(RepositoryFilterPattern pattern);

        /// <summary>
        /// Tests a pattern against a repository name to see if it would match
        /// </summary>
        bool TestPattern(string repositoryName, string owner, RepositoryFilterPattern pattern);
    }
}