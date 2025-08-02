namespace AICodeReviewer.Models.Configuration
{
    /// <summary>
    /// Repository information for GitHub configuration
    /// </summary>
    public class RepositoryInfo
    {
        public string Owner { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public string DefaultBranch { get; set; } = "main";
        public int StarCount { get; set; }
        public int ForkCount { get; set; }
        public string FullName => $"{Owner}/{Name}";
        public string DisplayName => string.IsNullOrEmpty(Owner) || string.IsNullOrEmpty(Name)
            ? "Not configured"
            : $"{Owner}/{Name}";
    }
}
