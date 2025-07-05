using Octokit;

namespace AICodeReviewer.Utils
{
    /// <summary>
    /// Utility methods for file operations and file type detection
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// Gets the filename from a file object (either GitHubCommitFile or PullRequestFile)
        /// </summary>
        public static string GetFileName(object file)
        {
            return file switch
            {
                GitHubCommitFile commitFile => commitFile.Filename,
                PullRequestFile prFile => prFile.FileName,
                _ => ""
            };
        }

        /// <summary>
        /// Gets the file status from a file object (either GitHubCommitFile or PullRequestFile)
        /// </summary>
        public static string GetFileStatus(object file)
        {
            return file switch
            {
                GitHubCommitFile commitFile => commitFile.Status,
                PullRequestFile prFile => prFile.Status,
                _ => ""
            };
        }

        /// <summary>
        /// Determines if a file is a code file based on its extension
        /// </summary>
        public static bool IsCodeFile(string fileName)
        {
            var codeExtensions = new[]
            {
                ".cs",
                ".vb",
                ".js",
                ".ts",
                ".py",
                ".java",
                ".cpp",
                ".c",
                ".h",
                ".php",
                ".rb",
                ".kt",
                ".swift",
                ".sql"
            };

            return codeExtensions.Any(
                ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
            );
        }

        /// <summary>
        /// Gets a human-readable file type description based on file extension
        /// </summary>
        public static string GetFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".cs" => "C#",
                ".vb" => "VB.NET",
                ".js" => "JavaScript",
                ".ts" => "TypeScript",
                ".py" => "Python",
                ".java" => "Java",
                ".cpp" or ".c" => "C/C++",
                ".php" => "PHP",
                ".rb" => "Ruby",
                ".kt" => "Kotlin",
                ".swift" => "Swift",
                ".sql" => "T-SQL",
                _ => "code"
            };
        }
    }
}
