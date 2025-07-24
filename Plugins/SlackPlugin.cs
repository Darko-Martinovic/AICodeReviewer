using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace AICodeReviewer.Plugins;

/// <summary>
/// Slack integration plugin for sending notifications and messages
/// </summary>
public class SlackPlugin
{
    private readonly ILogger<SlackPlugin> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;

    public SlackPlugin(ILogger<SlackPlugin> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _webhookUrl = configuration["Slack:WebhookUrl"] ?? throw new InvalidOperationException("Slack webhook URL not configured");
    }

    /// <summary>
    /// Sends a simple message to a Slack channel
    /// </summary>
    [KernelFunction, Description("Send a message to a Slack channel")]
    public async Task<string> SendMessageAsync(
        [Description("The message text to send")] string message,
        [Description("The channel to send to (optional)")] string? channel = null,
        [Description("Username to display as sender")] string username = "AI Code Reviewer")
    {
        try
        {
            var payload = new
            {
                text = message,
                channel = channel,
                username = username,
                icon_emoji = ":robot_face:"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_webhookUrl, content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("✅ Slack message sent successfully");
            return "Message sent successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send Slack message");
            return $"Failed to send message: {ex.Message}";
        }
    }

    /// <summary>
    /// Sends a rich message with attachments to Slack
    /// </summary>
    [KernelFunction, Description("Send a rich message with attachments to Slack")]
    public async Task<string> SendRichMessageAsync(
        [Description("The main message text")] string message,
        [Description("The title of the attachment")] string title,
        [Description("The color of the attachment (good, warning, danger, or hex)")] string color = "good",
        [Description("Additional fields as JSON")] string? fields = null,
        [Description("The channel to send to")] string? channel = null)
    {
        try
        {
            var attachment = new
            {
                title = title,
                text = message,
                color = color,
                fields = !string.IsNullOrEmpty(fields) ? JsonSerializer.Deserialize<object[]>(fields) : null
            };

            var payload = new
            {
                attachments = new[] { attachment },
                channel = channel,
                username = "AI Code Reviewer",
                icon_emoji = ":gear:"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_webhookUrl, content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("✅ Slack rich message sent successfully");
            return "Rich message sent successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send Slack rich message");
            return $"Failed to send rich message: {ex.Message}";
        }
    }

    /// <summary>
    /// Sends a code review summary to Slack
    /// </summary>
    [KernelFunction, Description("Send a code review summary to Slack")]
    public async Task<string> SendCodeReviewSummaryAsync(
        [Description("The PR number")] int prNumber,
        [Description("The PR title")] string prTitle,
        [Description("Number of issues found")] int issuesCount,
        [Description("Highest severity level")] string severity,
        [Description("The channel to send to")] string channel = "#code-reviews")
    {
        var color = severity.ToLower() switch
        {
            "critical" => "danger",
            "high" => "warning",
            "medium" => "warning",
            "low" => "good",
            _ => "good"
        };

        var emoji = severity.ToLower() switch
        {
            "critical" => ":red_circle:",
            "high" => ":warning:",
            "medium" => ":yellow_circle:",
            "low" => ":green_circle:",
            _ => ":white_circle:"
        };

        var message = $"{emoji} Code review completed for PR #{prNumber}: {prTitle}";
        var title = $"Found {issuesCount} issue(s) - Highest severity: {severity}";

        return await SendRichMessageAsync(message, title, color, null, channel);
    }
}
