using Octokit;

namespace AICodeReviewer.Services.Interfaces
{
    /// <summary>
    /// Interface for GitHub App authentication and operations
    /// </summary>
    public interface IGitHubAppService : IDisposable
    {
        /// <summary>
        /// Gets whether GitHub App authentication is configured and enabled
        /// </summary>
        bool IsAppAuthenticationEnabled { get; }

        /// <summary>
        /// Creates an authenticated GitHubClient using GitHub App credentials
        /// </summary>
        /// <returns>Authenticated GitHub client for the installation</returns>
        Task<GitHubClient> CreateAppAuthenticatedClientAsync();

        /// <summary>
        /// Gets repositories accessible to the GitHub App installation
        /// </summary>
        /// <returns>List of repositories accessible to the installation</returns>
        Task<IReadOnlyList<Repository>> GetInstallationRepositoriesAsync();

        /// <summary>
        /// Validates GitHub App configuration and connectivity
        /// </summary>
        /// <returns>True if the app is properly configured and can authenticate</returns>
        Task<bool> ValidateAppConfigurationAsync();

        /// <summary>
        /// Gets GitHub App installation information
        /// </summary>
        /// <returns>Installation details</returns>
        Task<Installation> GetInstallationAsync();

        /// <summary>
        /// Generates a JWT token for debugging purposes
        /// </summary>
        /// <returns>JWT token string</returns>
        string GenerateJwtTokenForDebugging();
    }
}