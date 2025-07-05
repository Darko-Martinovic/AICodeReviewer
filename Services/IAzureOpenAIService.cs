namespace AICodeReviewer.Services
{
    /// <summary>
    /// Interface for Azure OpenAI service operations
    /// </summary>
    public interface IAzureOpenAIService
    {
        Task<(List<string> issues, List<Models.DetailedIssue> detailedIssues, Models.Usage usage)> AnalyzeCodeAsync(string fileName, string fileContent);
    }
}
