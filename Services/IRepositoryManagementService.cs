using System.Collections.Generic;
using System.Threading.Tasks;
using AICodeReviewer.Models.Configuration;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Interface for managing repository selection and history
    /// </summary>
    public interface IRepositoryManagementService
    {
        /// <summary>
        /// Gets the current repository information
        /// </summary>
        Task<(string Owner, string Name)> GetCurrentRepositoryAsync();

        /// <summary>
        /// Prompts user to select a repository and returns the selection
        /// </summary>
        Task<(string Owner, string Name)> PromptForRepositoryAsync();

        /// <summary>
        /// Gets the list of recently used repositories
        /// </summary>
        Task<List<RepositoryInfo>> GetRepositoryHistoryAsync();

        /// <summary>
        /// Adds a repository to the history
        /// </summary>
        Task AddToHistoryAsync(string owner, string name, string description = "");

        /// <summary>
        /// Validates if a repository exists and is accessible
        /// </summary>
        Task<bool> ValidateRepositoryAsync(string owner, string name);

        /// <summary>
        /// Gets available repositories for the current user
        /// </summary>
        Task<List<RepositoryInfo>> GetAvailableRepositoriesAsync();
    }
}