namespace AICodeReviewer.Services.Interfaces
{
    /// <summary>
    /// Reviews one file for the commit, pull request and Train AI pages
    /// </summary>
    public interface ICodeAnalysisService
    {
        /// <param name="patch">The file's unified diff, to review only what it changed; null reviews the whole file</param>
        Task<(List<string> issues, List<Models.DetailedIssue> detailedIssues, Models.Usage usage)> AnalyzeCodeAsync(
            string fileName, string fileContent, string? patch = null);
    }
}
