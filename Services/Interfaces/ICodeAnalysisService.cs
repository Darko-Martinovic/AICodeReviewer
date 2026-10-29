namespace AICodeReviewer.Services.Interfaces
{
    /// <summary>
    /// Reviews one file for the commit, pull request and Train AI pages
    /// </summary>
    public interface ICodeAnalysisService
    {
        Task<(List<string> issues, List<Models.DetailedIssue> detailedIssues, Models.Usage usage)> AnalyzeCodeAsync(string fileName, string fileContent);
    }
}
