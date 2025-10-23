namespace AICodeReviewer.Models.Configuration;

/// <summary>
/// Configuration settings for Slack integration
/// </summary>
public class SlackSettings
{
    /// <summary>
    /// Slack webhook URL for incoming webhooks (optional if using bot token)
    /// </summary>
    public string WebhookUrl { get; set; } = "";

    /// <summary>
    /// Slack bot token (starts with xoxb- for bots or xoxe- for extended tokens)
    /// </summary>
    public string BotToken { get; set; } = "";

    /// <summary>
    /// Slack app token (if using socket mode)
    /// </summary>
    public string AppToken { get; set; } = "";

    /// <summary>
    /// Slack app ID
    /// </summary>
    public string AppId { get; set; } = "";

    /// <summary>
    /// Slack workspace ID or name
    /// </summary>
    public string WorkspaceId { get; set; } = "";

    /// <summary>
    /// Default channel for sending notifications
    /// </summary>
    public string DefaultChannel { get; set; } = "#code-reviews";

    /// <summary>
    /// Whether Slack notifications are enabled
    /// </summary>
    public bool EnableNotifications { get; set; } = true;

    /// <summary>
    /// Message formatting options
    /// </summary>
    public SlackMessageOptions MessageOptions { get; set; } = new();

    /// <summary>
    /// Error handling configuration
    /// </summary>
    public SlackErrorHandling ErrorHandling { get; set; } = new();
}

/// <summary>
/// Slack message formatting options
/// </summary>
public class SlackMessageOptions
{
    /// <summary>
    /// Username to display as the sender
    /// </summary>
    public string Username { get; set; } = "AI Code Reviewer";

    /// <summary>
    /// Icon emoji for the bot
    /// </summary>
    public string IconEmoji { get; set; } = ":robot_face:";

    /// <summary>
    /// Whether to use thread replies for follow-up messages
    /// </summary>
    public bool ThreadReplies { get; set; } = true;
}

/// <summary>
/// Slack error handling configuration
/// </summary>
public class SlackErrorHandling
{
    /// <summary>
    /// Number of retry attempts for failed requests
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to fail silently when Slack is unavailable
    /// </summary>
    public bool FailSilently { get; set; } = true;

    /// <summary>
    /// Whether to log errors to the application logger
    /// </summary>
    public bool LogErrors { get; set; } = true;
}