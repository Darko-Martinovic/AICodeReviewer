using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for managing dynamic prompts based on file language
    /// </summary>
    public class PromptManagementService : IPromptManagementService
    {
        private readonly ILanguageDetectionService _languageDetectionService;
        private readonly IConfigurationService _configurationService;
        private readonly PromptStore _promptStore;
        private readonly bool _useCustomPrompts;

        /// <param name="useCustomPrompts">Whether a prompt saved in settings overrides the versioned prompt; the lab turns this off</param>
        public PromptManagementService(
            ILanguageDetectionService languageDetectionService,
            IConfigurationService configurationService,
            PromptStore promptStore,
            bool useCustomPrompts = true)
        {
            _languageDetectionService = languageDetectionService ?? throw new ArgumentNullException(nameof(languageDetectionService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _promptStore = promptStore ?? throw new ArgumentNullException(nameof(promptStore));
            _useCustomPrompts = useCustomPrompts;
        }

        /// <summary>
        /// Gets the appropriate system prompt for the given file
        /// </summary>
        /// <param name="fileName">The name of the file to analyze</param>
        /// <returns>The system prompt for the file's language</returns>
        public string GetSystemPrompt(string fileName)
        {
            var languageKey = PromptLanguage(fileName);
            if (_useCustomPrompts)
            {
                var custom = CustomSystemPrompt(languageKey);
                if (!string.IsNullOrWhiteSpace(custom))
                {
                    return custom;
                }
            }
            return _promptStore.ActiveText(languageKey);
        }

        /// <summary>
        /// The prompt language of a file: its language key, or General for languages without prompts of their own
        /// </summary>
        public string PromptLanguage(string fileName) =>
            _languageDetectionService.IsLanguageSupported(fileName)
                ? _languageDetectionService.GetLanguageKey(fileName)
                : PromptStore.General;

        /// <summary>
        /// A prompt saved in settings (appsettings.json or Train AI), which overrides the versioned one
        /// </summary>
        private string CustomSystemPrompt(string languageKey)
        {
            var settings = _configurationService.Settings.AzureOpenAI;
            var languagePrompts = settings.LanguagePrompts;
            return languageKey switch
            {
                "CSharp" => languagePrompts.CSharp.SystemPrompt,
                "Java" => languagePrompts.Java.SystemPrompt,
                "VbNet" => languagePrompts.VbNet.SystemPrompt,
                "Sql" => languagePrompts.Sql.SystemPrompt,
                "JavaScript" => languagePrompts.JavaScript.SystemPrompt,
                "TypeScript" => languagePrompts.TypeScript.SystemPrompt,
                "React" => languagePrompts.React.SystemPrompt,
                _ => settings.SystemPrompt
            };
        }

        /// <summary>
        /// Gets the appropriate user prompt template for the given file
        /// </summary>
        /// <param name="fileName">The name of the file to analyze</param>
        /// <returns>The user prompt template for the file's language</returns>
        public string GetUserPromptTemplate(string fileName)
        {
            if (!_languageDetectionService.IsLanguageSupported(fileName))
            {
                // Fall back to default template
                return @"Please review this {fileType} file and provide detailed analysis:

File: {fileName}
Content Length: {contentLength} characters
{truncationNotice}

```
{fileContent}
{truncationIndicator}
```

Note: {analysisNote}";
            }

            var languageKey = _languageDetectionService.GetLanguageKey(fileName);
            var languagePrompts = _configurationService.Settings.AzureOpenAI.LanguagePrompts;

            return languageKey switch
            {
                "CSharp" => languagePrompts.CSharp.UserPromptTemplate,
                "VbNet" => languagePrompts.VbNet.UserPromptTemplate,
                "Sql" => languagePrompts.Sql.UserPromptTemplate,
                "JavaScript" => languagePrompts.JavaScript.UserPromptTemplate,
                "TypeScript" => languagePrompts.TypeScript.UserPromptTemplate,
                "React" => languagePrompts.React.UserPromptTemplate,
                _ => @"Please review this {fileType} file and provide detailed analysis:

File: {fileName}
Content Length: {contentLength} characters
{truncationNotice}

```
{fileContent}
{truncationIndicator}
```

Note: {analysisNote}"
            };
        }

        /// <summary>
        /// Formats the user prompt with the given parameters
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <param name="fileContent">The content of the file</param>
        /// <param name="contentLimit">The content limit for truncation</param>
        /// <returns>The formatted user prompt</returns>
        public string FormatUserPrompt(string fileName, string fileContent, int contentLimit)
        {
            var template = GetUserPromptTemplate(fileName);
            var fileType = _languageDetectionService.DetectLanguage(fileName);
            var contentLength = fileContent.Length;
            var isTruncated = contentLength > contentLimit;
            var truncatedContent = isTruncated ? fileContent.Substring(0, contentLimit) : fileContent;

            var truncationNotice = isTruncated
                ? $"(Showing first {contentLimit:N0} characters of {contentLength:N0} total)"
                : "";

            var truncationIndicator = isTruncated
                ? "\n... [Content truncated for analysis] ..."
                : "";

            var analysisNote = isTruncated
                ? "This is a partial view of a larger file. Focus on identifying patterns, architectural issues, and code quality problems that are visible in this section."
                : "This is the complete file content.";

            return template
                .Replace("{fileName}", fileName)
                .Replace("{fileType}", fileType)
                .Replace("{contentLength}", contentLength.ToString())
                .Replace("{truncationNotice}", truncationNotice)
                .Replace("{fileContent}", truncatedContent)
                .Replace("{truncationIndicator}", truncationIndicator)
                .Replace("{analysisNote}", analysisNote);
        }

        /// <summary>
        /// Gets whether the file has language-specific prompts
        /// </summary>
        /// <param name="fileName">The name of the file to check</param>
        /// <returns>True if the file has specialized prompts</returns>
        public bool HasLanguageSpecificPrompts(string fileName)
        {
            return _languageDetectionService.IsLanguageSupported(fileName);
        }
    }
}