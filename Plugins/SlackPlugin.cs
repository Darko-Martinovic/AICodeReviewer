using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Plugins;

/// <summary>
/// Slack integration plugin for sending notifications and messages
/// </summary>
public class SlackPlugin
{
    private readonly ILogger<SlackPlugin> _logger;
    private readonly HttpClient _httpClient;
    private readonly SlackSettings _slackSettings;
    private readonly bool _isConfigured;

    public SlackPlugin(ILogger<SlackPlugin> logger, HttpClient httpClient, IConfigurationService configurationService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _slackSettings = configurationService.Settings.Slack;

        // Check if Slack is properly configured
        _isConfigured = IsSlackConfigured();

        if (!_isConfigured && _slackSettings.ErrorHandling.LogErrors)
        {
            _logger.LogWarning("⚠️ Slack integration is not properly configured. Notifications will be disabled.");
        }
    }

    private bool IsSlackConfigured()
    {
        return !string.IsNullOrEmpty(_slackSettings.BotToken) || !string.IsNullOrEmpty(_slackSettings.WebhookUrl);
    }

    /// <summary>
    /// Sends a simple message to a Slack channel
    /// </summary>
    [KernelFunction, Description("Send a message to a Slack channel")]
    public async Task<string> SendMessageAsync(
        [Description("The message text to send")] string message,
        [Description("The channel to send to (optional)")] string? channel = null,
        [Description("Username to display as sender")] string? username = null)
    {
        if (!_slackSettings.EnableNotifications)
        {
            _logger.LogInformation("📴 Slack notifications are disabled in configuration");
            return "Slack notifications are disabled";
        }

        if (!_isConfigured)
        {
            var errorMsg = "Slack is not properly configured (missing BotToken or WebhookUrl)";
            if (_slackSettings.ErrorHandling.LogErrors)
            {
                _logger.LogWarning("⚠️ {ErrorMessage}", errorMsg);
            }

            if (_slackSettings.ErrorHandling.FailSilently)
            {
                return "Slack configuration incomplete - skipped";
            }

            throw new InvalidOperationException(errorMsg);
        }

        var attempts = 0;
        var maxAttempts = _slackSettings.ErrorHandling.RetryAttempts;

        while (attempts < maxAttempts)
        {
            try
            {
                attempts++;

                if (!string.IsNullOrEmpty(_slackSettings.WebhookUrl))
                {
                    return await SendViaWebhookAsync(message, channel, username);
                }
                else if (!string.IsNullOrEmpty(_slackSettings.BotToken))
                {
                    return await SendViaBotTokenAsync(message, channel, username);
                }

                throw new InvalidOperationException("No valid Slack configuration method available");
            }
            catch (Exception ex)
            {
                if (attempts >= maxAttempts)
                {
                    if (_slackSettings.ErrorHandling.LogErrors)
                    {
                        _logger.LogError(ex, "❌ Failed to send Slack message after {Attempts} attempts", maxAttempts);
                    }

                    if (_slackSettings.ErrorHandling.FailSilently)
                    {
                        return $"Failed to send message (silently handled): {ex.Message}";
                    }

                    throw;
                }

                if (_slackSettings.ErrorHandling.LogErrors)
                {
                    _logger.LogWarning("⚠️ Slack message attempt {Attempt} failed: {Error}. Retrying...", attempts, ex.Message);
                }

                await Task.Delay(_slackSettings.ErrorHandling.RetryDelayMs);
            }
        }

        return "Unexpected error in retry logic";
    }

    private async Task<string> SendViaWebhookAsync(string message, string? channel, string? username)
    {
        var payload = new
        {
            text = message,
            channel = channel ?? _slackSettings.DefaultChannel,
            username = username ?? _slackSettings.MessageOptions.Username,
            icon_emoji = _slackSettings.MessageOptions.IconEmoji
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_slackSettings.WebhookUrl, content);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("✅ Slack message sent successfully via webhook");
        return "Message sent successfully via webhook";
    }

    private async Task<string> SendViaBotTokenAsync(string message, string? channel, string? username)
    {
        // For bot token, we would use the Slack Web API (chat.postMessage)
        // This requires the Slack.Net package or similar, but for now we'll simulate
        _logger.LogInformation("🤖 Would send via Bot Token API (not yet implemented)");

        // Simulate API call
        await Task.Delay(100);

        return "Message sent successfully via bot token (simulated)";
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
        if (!_slackSettings.EnableNotifications || !_isConfigured)
        {
            return await SendMessageAsync($"{title}: {message}", channel);
        }

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
                channel = channel ?? _slackSettings.DefaultChannel,
                username = _slackSettings.MessageOptions.Username,
                icon_emoji = _slackSettings.MessageOptions.IconEmoji
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_slackSettings.WebhookUrl, content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("✅ Slack rich message sent successfully");
            return "Rich message sent successfully";
        }
        catch (Exception ex)
        {
            if (_slackSettings.ErrorHandling.LogErrors)
            {
                _logger.LogError(ex, "❌ Failed to send Slack rich message");
            }

            if (_slackSettings.ErrorHandling.FailSilently)
            {
                return $"Failed to send rich message (silently handled): {ex.Message}";
            }

            throw;
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
        [Description("The channel to send to")] string? channel = null)
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

        return await SendRichMessageAsync(message, title, color, null, channel ?? _slackSettings.DefaultChannel);
    }

    /// <summary>
    /// Sends a commit review summary to Slack
    /// </summary>
    [KernelFunction, Description("Send a commit review summary to Slack")]
    public async Task<string> SendCommitReviewSummaryAsync(
        [Description("The commit SHA")] string commitSha,
        [Description("The commit message")] string commitMessage,
        [Description("The author name")] string author,
        [Description("The repository name")] string repository,
        [Description("Number of issues found")] int issuesCount,
        [Description("Highest severity level")] string severity,
        [Description("The channel to send to")] string? channel = null)
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

        var shortSha = commitSha.Length > 8 ? commitSha.Substring(0, 8) : commitSha;
        var shortMessage = commitMessage.Length > 60 ? $"{commitMessage.Substring(0, 60)}..." : commitMessage;

        var message = $"{emoji} **Commit Review Complete**\n\n" +
                     $"**Commit:** `{shortSha}`\n" +
                     $"**Author:** {author}\n" +
                     $"**Repository:** {repository}\n" +
                     $"**Message:** {shortMessage}\n\n" +
                     $"📊 **Issues Found:** {issuesCount}\n" +
                     $"📈 **Severity:** {severity}\n\n" +
                     (issuesCount > 0
                         ? "⚠️ Please review the identified issues"
                         : "✅ No issues found - great work!");

        return await SendMessageAsync(message, channel ?? _slackSettings.DefaultChannel);
    }

    /// <summary>
    /// Checks if Slack integration is properly configured and available
    /// </summary>
    [KernelFunction, Description("Check if Slack integration is available")]
    public async Task<string> CheckSlackStatusAsync()
    {
        if (!_slackSettings.EnableNotifications)
        {
            return "Slack notifications are disabled in configuration";
        }

        if (!_isConfigured)
        {
            return "Slack is not properly configured (missing BotToken or WebhookUrl)";
        }

        try
        {
            // Test the connection with a simple ping
            await SendMessageAsync("🏃‍♂️ Slack integration test - connection successful!", _slackSettings.DefaultChannel);
            return "Slack integration is working correctly";
        }
        catch (Exception ex)
        {
            return $"Slack integration test failed: {ex.Message}";
        }
    }
}
