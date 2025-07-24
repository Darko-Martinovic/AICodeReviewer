using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AICodeReviewer.Plugins;

/// <summary>
/// Semantic Kernel plugin for Jira operations
/// </summary>
public class JiraPlugin
{
    [KernelFunction]
    [Description("Creates a Jira ticket for code review issues")]
    public async Task<string> CreateTicket(
        [Description("Jira project key")] string project,
        [Description("Issue type")] string issueType,
        [Description("Issue summary")] string summary,
        [Description("Issue description")] string description,
        [Description("Priority level")] string priority = "Medium",
        [Description("Assignee username")] string assignee = "")
    {
        try
        {
            // Simulate creating Jira ticket
            var ticketId = $"{project}-{Random.Shared.Next(1000, 9999)}";
            
            Console.WriteLine($"🎫 Jira Ticket Created:");
            Console.WriteLine($"Ticket ID: {ticketId}");
            Console.WriteLine($"Project: {project}");
            Console.WriteLine($"Type: {issueType}");
            Console.WriteLine($"Priority: {priority}");
            Console.WriteLine($"Assignee: {assignee}");
            Console.WriteLine($"Summary: {summary}");
            Console.WriteLine($"Description: {description}");
            
            // In real implementation:
            // var ticket = await _jiraService.CreateIssueAsync(project, issueType, summary, description, priority, assignee);
            
            await Task.Delay(200); // Simulate async operation
            
            return $"Successfully created Jira ticket {ticketId}";
        }
        catch (Exception ex)
        {
            return $"Error creating Jira ticket: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Updates an existing Jira ticket")]
    public async Task<string> UpdateTicket(
        [Description("Jira ticket ID")] string ticketId,
        [Description("Update description")] string update,
        [Description("New status")] string status = "")
    {
        try
        {
            // Simulate updating Jira ticket
            Console.WriteLine($"🔄 Jira Ticket Updated:");
            Console.WriteLine($"Ticket ID: {ticketId}");
            Console.WriteLine($"Update: {update}");
            if (!string.IsNullOrEmpty(status))
                Console.WriteLine($"New Status: {status}");
            
            await Task.Delay(100); // Simulate async operation
            
            return $"Successfully updated Jira ticket {ticketId}";
        }
        catch (Exception ex)
        {
            return $"Error updating Jira ticket: {ex.Message}";
        }
    }
}
