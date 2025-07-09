namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for managing dynamic prompts based on file language
    /// </summary>
    public interface IPromptManagementService
    {
        /// <summary>
        /// Gets the appropriate system prompt for the given file
        /// </summary>
        /// <param name="fileName">The name of the file to analyze</param>
        /// <returns>The system prompt for the file's language</returns>
        string GetSystemPrompt(string fileName);

        /// <summary>
        /// Gets the appropriate user prompt template for the given file
        /// </summary>
        /// <param name="fileName">The name of the file to analyze</param>
        /// <returns>The user prompt template for the file's language</returns>
        string GetUserPromptTemplate(string fileName);

        /// <summary>
        /// Formats the user prompt with the given parameters
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <param name="fileContent">The content of the file</param>
        /// <param name="contentLimit">The content limit for truncation</param>
        /// <returns>The formatted user prompt</returns>
        string FormatUserPrompt(string fileName, string fileContent, int contentLimit);

        /// <summary>
        /// Gets whether the file has language-specific prompts
        /// </summary>
        /// <param name="fileName">The name of the file to check</param>
        /// <returns>True if the file has specialized prompts</returns>
        bool HasLanguageSpecificPrompts(string fileName);
    }
}