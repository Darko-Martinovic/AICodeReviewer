using Microsoft.SemanticKernel;
using System.ComponentModel;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Models;

namespace AICodeReviewer.Plugins;

/// <summary>
/// Semantic Kernel plugin for code review operations
/// </summary>
public class CodeReviewPlugin
{
    private readonly ICodeReviewService _codeReviewService;
    private readonly IGitHubService _gitHubService;

    public CodeReviewPlugin(ICodeReviewService codeReviewService, IGitHubService gitHubService)
    {
        _codeReviewService = codeReviewService;
        _gitHubService = gitHubService;
    }

    [KernelFunction]
    [Description("Reviews a pull request and returns detailed analysis")]
    public async Task<string> ReviewPullRequest(
        [Description("Pull request number")] int prNumber,
        [Description("Include security check")] bool includeSecurityCheck = true,
        [Description("Include complexity analysis")] bool includeComplexityAnalysis = true,
        [Description("Include test coverage")] bool includeTestCoverage = true)
    {
        try
        {
            var review = await _codeReviewService.ReviewPullRequestAsync(prNumber);
            
            var result = new
            {
                PullRequestNumber = prNumber,
                Summary = GenerateSummary(review),
                Issues = review.DetailedIssues.Select(i => new
                {
                    Severity = i.Severity,
                    File = i.FileName,
                    Line = i.LineNumber,
                    Message = i.Description,
                    Suggestion = i.Recommendation
                }),
                SecurityIssues = review.DetailedIssues.Where(i => 
                    i.Category?.ToLower().Contains("security") == true),
                Complexity = DetermineComplexity(review),
                Metrics = new
                {
                    FilesReviewed = review.Metrics.FilesReviewed,
                    IssuesFound = review.Metrics.IssuesFound,
                    Duration = review.Metrics.Duration.TotalSeconds
                }
            };

            return System.Text.Json.JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return $"Error reviewing pull request: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Reviews a commit and returns detailed analysis")]
    public async Task<string> ReviewCommit(
        [Description("Commit SHA")] string commitSha,
        [Description("Include security check")] bool includeSecurityCheck = true,
        [Description("Include complexity analysis")] bool includeComplexityAnalysis = false,
        [Description("Include test coverage")] bool includeTestCoverage = false)
    {
        try
        {
            var review = await _codeReviewService.ReviewCommitAsync(commitSha);
            var commit = await _gitHubService.GetCommitAsync(commitSha);
            
            var result = new
            {
                CommitSha = commitSha,
                Author = commit.Commit.Author.Name,
                Message = commit.Commit.Message,
                Summary = GenerateSummary(review),
                Issues = review.DetailedIssues.Select(i => new
                {
                    Severity = i.Severity,
                    File = i.FileName,
                    Line = i.LineNumber,
                    Message = i.Description,
                    Suggestion = i.Recommendation
                }),
                SecurityIssues = review.DetailedIssues.Where(i => 
                    i.Category?.ToLower().Contains("security") == true),
                Complexity = DetermineComplexity(review),
                Metrics = new
                {
                    FilesReviewed = review.Metrics.FilesReviewed,
                    IssuesFound = review.Metrics.IssuesFound,
                    Duration = review.Metrics.Duration.TotalSeconds
                }
            };

            return System.Text.Json.JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            return $"Error reviewing commit: {ex.Message}";
        }
    }

    private string GenerateSummary(CodeReviewResult review)
    {
        if (review.HasIssues)
        {
            var highSeverityCount = review.DetailedIssues.Count(i => 
                i.Severity?.ToLower() == "high" || i.Severity?.ToLower() == "critical");
            var mediumSeverityCount = review.DetailedIssues.Count(i => 
                i.Severity?.ToLower() == "medium");
            var lowSeverityCount = review.DetailedIssues.Count(i => 
                i.Severity?.ToLower() == "low");

            var summaryParts = new List<string>();
            if (highSeverityCount > 0) summaryParts.Add($"{highSeverityCount} high-priority issue(s)");
            if (mediumSeverityCount > 0) summaryParts.Add($"{mediumSeverityCount} medium-priority issue(s)");
            if (lowSeverityCount > 0) summaryParts.Add($"{lowSeverityCount} low-priority issue(s)");

            return $"Review completed. Found {string.Join(", ", summaryParts)}. " +
                   $"Total files reviewed: {review.Metrics.FilesReviewed}.";
        }
        else
        {
            return $"Review completed successfully. No issues found in {review.Metrics.FilesReviewed} file(s). Great work!";
        }
    }

    private string DetermineComplexity(CodeReviewResult review)
    {
        var issueCount = review.IssueCount;
        var filesReviewed = review.Metrics.FilesReviewed;
        
        if (issueCount == 0) return "Low";
        
        var issuesPerFile = filesReviewed > 0 ? (double)issueCount / filesReviewed : issueCount;
        
        if (issuesPerFile >= 3) return "High";
        if (issuesPerFile >= 1.5) return "Medium";
        return "Low";
    }
}
