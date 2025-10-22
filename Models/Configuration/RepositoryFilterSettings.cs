namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Repository filtering configuration to limit which repositories are shown
    /// </summary>
    public class RepositoryFilterSettings
    {
        /// <summary>
        /// List of repository patterns to include
        /// Supports wildcards using asterisk (*) - e.g., "ai*" includes all repos starting with "ai"
        /// </summary>
        public List<RepositoryFilterPattern> IncludePatterns { get; set; } = new();

        /// <summary>
        /// List of repository patterns to explicitly exclude
        /// Supports wildcards using asterisk (*) - e.g., "test*" excludes all repos starting with "test"
        /// </summary>
        public List<RepositoryFilterPattern> ExcludePatterns { get; set; } = new();

        /// <summary>
        /// Whether to apply filters (if false, all repositories are shown)
        /// </summary>
        public bool EnableFiltering { get; set; } = false;

        /// <summary>
        /// Default filter mode when no patterns are specified
        /// </summary>
        public FilterMode DefaultMode { get; set; } = FilterMode.ShowAll;
    }

    /// <summary>
    /// Repository filter pattern with support for multiple Git hosting providers
    /// </summary>
    public class RepositoryFilterPattern
    {
        /// <summary>
        /// Pattern to match repository names (supports wildcards with *)
        /// Examples: "ai*", "*backend*", "project-name"
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Git hosting provider (e.g., "github", "gitlab", "bitbucket", "azure-devops")
        /// If null or empty, applies to all providers
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Optional owner/organization filter
        /// If specified, only repositories from this owner will be matched
        /// </summary>
        public string? Owner { get; set; }

        /// <summary>
        /// Whether this pattern is case-sensitive
        /// </summary>
        public bool CaseSensitive { get; set; } = false;

        /// <summary>
        /// Description of this filter pattern for display purposes
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Filter mode for repository display
    /// </summary>
    public enum FilterMode
    {
        /// <summary>
        /// Show all repositories (no filtering)
        /// </summary>
        ShowAll,

        /// <summary>
        /// Show only repositories matching include patterns
        /// </summary>
        IncludeOnly,

        /// <summary>
        /// Show all repositories except those matching exclude patterns
        /// </summary>
        ExcludeMatching
    }
}