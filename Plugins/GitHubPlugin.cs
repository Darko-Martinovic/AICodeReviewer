using Microsoft.SemanticKernel;
using System.ComponentModel;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Plugins;

/// <summary>
/// Semantic Kernel plugin for GitHub operations
/// </summary>
public class GitHubPlugin
{
    private readonly IGitHubService _gitHubService;

    public GitHubPlugin(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    [KernelFunction]
    [Description("Posts a comment on a pull request")]
    public async Task<string> PostPullRequestComment(
        [Description("Pull request number")] int prNumber,
        [Description("Comment content")] string content,
        [Description("Template to use")] string template = "",
        [Description("Review data for template")] string reviewData = "")
    {
        try
        {
            // For now, simulate posting comment (would integrate with Octokit)
            Console.WriteLine($"📝 GitHub Comment Posted on PR #{prNumber}:");
            Console.WriteLine($"Content: {content}");
            
            // In real implementation:
            // await _gitHubService.PostPullRequestCommentAsync(prNumber, content);
            
            return $"Successfully posted comment on PR #{prNumber}";
        }
        catch (Exception ex)
        {
            return $"Error posting GitHub comment: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Sets the status of a commit")]
    public async Task<string> SetCommitStatus(
        [Description("Commit SHA")] string commitSha,
        [Description("Status context")] string context,
        [Description("Status description")] string description,
        [Description("Status state")] string state = "success")
    {
        try
        {
            // Simulate setting commit status
            Console.WriteLine($"✅ Commit Status Set:");
            Console.WriteLine($"SHA: {commitSha}");
            Console.WriteLine($"Context: {context}");
            Console.WriteLine($"State: {state}");
            Console.WriteLine($"Description: {description}");
            
            // In real implementation:
            // await _gitHubService.SetCommitStatusAsync(commitSha, context, state, description);
            
            return $"Successfully set status for commit {commitSha}";
        }
        catch (Exception ex)
        {
            return $"Error setting commit status: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Sets pull request approval status")]
    public async Task<string> SetPullRequestStatus(
        [Description("Pull request number")] int prNumber,
        [Description("Status (approved/changes_requested/commented)")] string status,
        [Description("Review message")] string message)
    {
        try
        {
            // Simulate setting PR status
            Console.WriteLine($"🔍 PR Review Status Set:");
            Console.WriteLine($"PR: #{prNumber}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Message: {message}");
            
            // In real implementation:
            // await _gitHubService.SetPullRequestReviewAsync(prNumber, status, message);
            
            return $"Successfully set status '{status}' for PR #{prNumber}";
        }
        catch (Exception ex)
        {
            return $"Error setting PR status: {ex.Message}";
        }
    }
}
