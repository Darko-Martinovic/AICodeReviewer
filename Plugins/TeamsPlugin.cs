using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AICodeReviewer.Plugins;

/// <summary>
/// Semantic Kernel plugin for Microsoft Teams operations
/// </summary>
public class TeamsPlugin
{
    [KernelFunction]
    [Description("Sends an adaptive card to a Teams channel")]
    public async Task<string> SendAdaptiveCard(
        [Description("Teams channel name")] string channel,
        [Description("Card content")] string content,
        [Description("Template to use")] string template = "",
        [Description("Whether to mention team")] bool mentionTeam = false)
    {
        try
        {
            // Simulate sending Teams adaptive card
            Console.WriteLine($"📢 Teams Adaptive Card Sent:");
            Console.WriteLine($"Channel: {channel}");
            Console.WriteLine($"Template: {template}");
            Console.WriteLine($"Mention Team: {mentionTeam}");
            Console.WriteLine($"Content: {content}");
            
            // In real implementation, would use Teams SDK:
            // await _teamsService.SendAdaptiveCardAsync(channel, cardContent);
            
            await Task.Delay(100); // Simulate async operation
            
            return $"Successfully sent adaptive card to {channel}";
        }
        catch (Exception ex)
        {
            return $"Error sending Teams adaptive card: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Sends a message to a Teams channel")]
    public async Task<string> SendMessage(
        [Description("Teams channel name")] string channel,
        [Description("Message content")] string content,
        [Description("Template to use")] string template = "",
        [Description("Whether to aggregate messages")] bool aggregate = false)
    {
        try
        {
            // Simulate sending Teams message
            Console.WriteLine($"💬 Teams Message Sent:");
            Console.WriteLine($"Channel: {channel}");
            Console.WriteLine($"Template: {template}");
            Console.WriteLine($"Aggregate: {aggregate}");
            Console.WriteLine($"Content: {content}");
            
            await Task.Delay(100); // Simulate async operation
            
            return $"Successfully sent message to {channel}";
        }
        catch (Exception ex)
        {
            return $"Error sending Teams message: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Sends an urgent message to a Teams channel")]
    public async Task<string> SendUrgentMessage(
        [Description("Teams channel name")] string channel,
        [Description("Message content")] string content,
        [Description("Template to use")] string template = "",
        [Description("Whether to mention security team")] bool mentionSecurityTeam = false)
    {
        try
        {
            // Simulate sending urgent Teams message
            Console.WriteLine($"🚨 URGENT Teams Message Sent:");
            Console.WriteLine($"Channel: {channel}");
            Console.WriteLine($"Template: {template}");
            Console.WriteLine($"Mention Security Team: {mentionSecurityTeam}");
            Console.WriteLine($"Content: {content}");
            
            await Task.Delay(100); // Simulate async operation
            
            return $"Successfully sent urgent message to {channel}";
        }
        catch (Exception ex)
        {
            return $"Error sending urgent Teams message: {ex.Message}";
        }
    }
}
