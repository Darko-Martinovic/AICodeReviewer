namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for detecting programming language based on file extension
    /// </summary>
    public interface ILanguageDetectionService
    {
        /// <summary>
        /// Detects the programming language based on file extension
        /// </summary>
        /// <param name="fileName">The name of the file to analyze</param>
        /// <returns>The detected programming language</returns>
        string DetectLanguage(string fileName);

        /// <summary>
        /// Gets whether the language is supported for specialized prompts
        /// </summary>
        /// <param name="fileName">The name of the file to check</param>
        /// <returns>True if the language has specialized prompts</returns>
        bool IsLanguageSupported(string fileName);

        /// <summary>
        /// Gets the language key used for configuration lookup
        /// </summary>
        /// <param name="fileName">The name of the file to analyze</param>
        /// <returns>The language key for configuration</returns>
        string GetLanguageKey(string fileName);
    }
}