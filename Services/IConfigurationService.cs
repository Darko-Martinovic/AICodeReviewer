using AICodeReviewer.Models.Configuration;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Interface for configuration service operations
    /// </summary>
    public interface IConfigurationService
    {
        AppSettings Settings { get; }
        void DisplayConfigurationSummary();
    }
}
