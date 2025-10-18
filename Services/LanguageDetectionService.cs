using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for detecting programming language based on file extension
    /// </summary>
    public class LanguageDetectionService : ILanguageDetectionService
    {
        private static readonly Dictionary<string, string> _languageMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // C# files
            { ".cs", "CSharp" },
            { ".csx", "CSharp" },
            
            // Java files
            { ".java", "Java" },
            
            // VB.NET files
            { ".vb", "VbNet" },
            { ".vbs", "VbNet" },
            
            // SQL files
            { ".sql", "Sql" },
            { ".tsql", "Sql" },
            
            // JavaScript files
            { ".js", "JavaScript" },
            { ".jsx", "JavaScript" },
            { ".mjs", "JavaScript" },
            
            // TypeScript files
            { ".ts", "TypeScript" },
            { ".tsx", "TypeScript" },
            
            // React files (JSX/TSX are handled above, but we can detect React-specific patterns)
            // Note: .jsx and .tsx are already mapped above, so we don't need to duplicate them
            
            // Other common languages (not prioritized)
            { ".py", "Python" },
            { ".cpp", "C++" },
            { ".c", "C" },
            { ".php", "PHP" },
            { ".rb", "Ruby" },
            { ".kt", "Kotlin" },
            { ".swift", "Swift" },
            { ".go", "Go" },
            { ".rs", "Rust" },
            { ".scala", "Scala" },
            { ".pl", "Perl" },
            { ".sh", "Bash" },
            { ".ps1", "PowerShell" },
            { ".bat", "Batch" },
            { ".cmd", "Batch" },
            { ".html", "HTML" },
            { ".htm", "HTML" },
            { ".css", "CSS" },
            { ".scss", "SCSS" },
            { ".sass", "Sass" },
            { ".less", "Less" },
            { ".xml", "XML" },
            { ".json", "JSON" },
            { ".yaml", "YAML" },
            { ".yml", "YAML" },
            { ".toml", "TOML" },
            { ".ini", "INI" },
            { ".conf", "Config" },
            { ".config", "Config" }
        };

        private static readonly HashSet<string> _supportedLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            "CSharp",
            "Java",
            "VbNet",
            "Sql",
            "JavaScript",
            "TypeScript",
            "React"
        };

        /// <summary>
        /// Detects the programming language based on file extension
        /// </summary>
        /// <param name="fileName">The name of the file to analyze</param>
        /// <returns>The detected programming language</returns>
        public string DetectLanguage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "Unknown";

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
                return "Unknown";

            return _languageMap.TryGetValue(extension, out var language) ? language : "Unknown";
        }

        /// <summary>
        /// Gets whether the language is supported for specialized prompts
        /// </summary>
        /// <param name="fileName">The name of the file to check</param>
        /// <returns>True if the language has specialized prompts</returns>
        public bool IsLanguageSupported(string fileName)
        {
            var language = DetectLanguage(fileName);
            return _supportedLanguages.Contains(language);
        }

        /// <summary>
        /// Gets the language key used for configuration lookup
        /// </summary>
        /// <param name="fileName">The name of the file to analyze</param>
        /// <returns>The language key for configuration</returns>
        public string GetLanguageKey(string fileName)
        {
            var language = DetectLanguage(fileName);

            // Special handling for React files (JSX/TSX)
            if (language == "JavaScript" || language == "TypeScript")
            {
                var extension = Path.GetExtension(fileName).ToLower();
                if (extension == ".jsx" || extension == ".tsx")
                {
                    return "React";
                }
            }

            return language;
        }
    }
}