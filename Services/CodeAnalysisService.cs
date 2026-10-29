using AICodeReviewer.Models;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Gives the app's pages the reviewer's findings in the shape they already show
    /// </summary>
    public class CodeAnalysisService(IReviewer reviewer, ILogger<CodeAnalysisService> logger) : ICodeAnalysisService
    {
        public async Task<(List<string> issues, List<DetailedIssue> detailedIssues, Usage usage)> AnalyzeCodeAsync(string fileName, string fileContent, string? patch = null)
        {
            try
            {
                var change = patch is null ? null : LineDiff.FromPatch(patch);
                var review = await reviewer.ReviewFileAsync(fileName, fileContent, change: change);
                logger.LogInformation(
                    "Reviewed {FileName} with {Model}: {Findings} findings, {Input} input and {Output} output tokens, ${Cost:F4}",
                    fileName, review.Model, review.Findings.Count, review.InputTokens, review.OutputTokens, review.Cost);

                var detailedIssues = review.Findings.Select(f => new DetailedIssue
                {
                    FileName = fileName,
                    Category = f.Category,
                    Severity = f.Severity,
                    Title = f.Title,
                    Description = f.Explanation,
                    Recommendation = f.Suggestion,
                    LineNumber = f.Line
                }).ToList();

                var usage = new Usage
                {
                    prompt_tokens = review.InputTokens,
                    completion_tokens = review.OutputTokens,
                    total_tokens = review.InputTokens + review.OutputTokens,
                    cost = review.Cost
                };

                return (detailedIssues.Select(i => $"[{i.Severity}] {i.Title}").ToList(), detailedIssues, usage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reviewing {FileName} failed", fileName);
                return (new List<string> { $"AI analysis failed: {ex.Message}" }, new List<DetailedIssue>(), new Usage());
            }
        }
    }
}
