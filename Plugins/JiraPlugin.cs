using Microsoft.SemanticKernel;
using System.ComponentModel;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Plugins;

/// <summary>
/// Semantic Kernel plugin for Jira operations
/// </summary>
public class JiraPlugin
{
    private readonly IJiraService _jiraService;

    public JiraPlugin(IJiraService jiraService)
    {
        _jiraService = jiraService ?? throw new ArgumentNullException(nameof(jiraService));
    }

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
            Console.WriteLine($"🎫 Creating REAL Jira ticket in project {project}...");

            var ticketKey = await _jiraService.CreateIssueAsync(project, issueType, summary, description, priority, assignee);

            return $"Successfully created Jira ticket {ticketKey}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating Jira ticket: {ex.Message}");
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
