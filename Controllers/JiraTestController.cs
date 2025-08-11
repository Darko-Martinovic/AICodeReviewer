using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Controllers
{
    /// <summary>
    /// Test controller for JIRA integration
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class JiraTestController : ControllerBase
    {
        private readonly IJiraService _jiraService;

        public JiraTestController(IJiraService jiraService)
        {
            _jiraService = jiraService;
        }

        /// <summary>
        /// Tests JIRA configuration and creates a test ticket
        /// </summary>
        [HttpPost("create-test-ticket")]
        public async Task<IActionResult> CreateTestTicket()
        {
            try
            {
                Console.WriteLine("🧪 Testing JIRA ticket creation...");

                var ticketKey = await _jiraService.CreateIssueAsync(
                    project: "OPS",
                    issueType: "Task",
                    summary: "TEST: JIRA Integration Test",
                    description: "This is a test ticket created to verify JIRA integration is working correctly. This ticket was created from the AI Code Reviewer system test endpoint.",
                    priority: "Medium",
                    assignee: ""
                );

                return Ok(new
                {
                    Success = true,
                    Message = "JIRA test ticket created successfully",
                    TicketKey = ticketKey,
                    JiraUrl = $"{Environment.GetEnvironmentVariable("JIRA_BASE_URL")}/browse/{ticketKey}",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ JIRA test failed: {ex.Message}");
                return BadRequest(new
                {
                    Success = false,
                    Error = ex.Message,
                    Details = ex.ToString(),
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Tests JIRA configuration without creating tickets
        /// </summary>
        [HttpGet("test-config")]
        public IActionResult TestJiraConfig()
        {
            try
            {
                var jiraBaseUrl = Environment.GetEnvironmentVariable("JIRA_BASE_URL");
                var jiraApiToken = Environment.GetEnvironmentVariable("JIRA_API_TOKEN");
                var jiraUserEmail = Environment.GetEnvironmentVariable("JIRA_USER_EMAIL");

                return Ok(new
                {
                    ConfigurationStatus = new
                    {
                        JiraBaseUrl = !string.IsNullOrEmpty(jiraBaseUrl) ? "✅ SET" : "❌ NOT SET",
                        JiraApiToken = !string.IsNullOrEmpty(jiraApiToken) ? "✅ SET" : "❌ NOT SET",
                        JiraUserEmail = !string.IsNullOrEmpty(jiraUserEmail) ? "✅ SET" : "❌ NOT SET"
                    },
                    Values = new
                    {
                        JiraBaseUrl = jiraBaseUrl,
                        JiraUserEmail = jiraUserEmail,
                        JiraApiTokenLength = jiraApiToken?.Length ?? 0
                    },
                    IsFullyConfigured = !string.IsNullOrEmpty(jiraBaseUrl) &&
                                     !string.IsNullOrEmpty(jiraApiToken) &&
                                     !string.IsNullOrEmpty(jiraUserEmail),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Tests extracting JIRA ticket keys from PR title
        /// </summary>
        [HttpPost("test-extract-tickets")]
        public IActionResult TestExtractTickets([FromBody] string prTitle)
        {
            try
            {
                var tickets = _jiraService.ExtractTicketKeysFromTitle(prTitle);

                return Ok(new
                {
                    PrTitle = prTitle,
                    ExtractedTickets = tickets,
                    TicketCount = tickets.Count,
                    TestResults = new
                    {
                        ContainsOPS1 = tickets.Contains("OPS-1"),
                        AllTickets = tickets
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Tests updating a specific JIRA ticket
        /// </summary>
        [HttpPost("update-ticket/{ticketKey}")]
        public async Task<IActionResult> UpdateTicket(string ticketKey, [FromBody] string updateMessage)
        {
            try
            {
                Console.WriteLine($"🧪 Testing JIRA ticket update for {ticketKey}...");

                // Use the service to update the ticket
                await _jiraService.UpdateTicketsWithReviewResultsAsync(
                    ticketKeys: new List<string> { ticketKey },
                    prNumber: "TEST-UPDATE",
                    author: "AI Code Reviewer Test",
                    issueCount: 1,
                    reviewedFiles: new List<string> { "Test file update" },
                    topIssues: new List<string> { updateMessage ?? "Test update from JIRA controller" },
                    detailedIssues: null
                );

                return Ok(new
                {
                    Success = true,
                    Message = $"JIRA ticket {ticketKey} updated successfully",
                    TicketKey = ticketKey,
                    UpdateMessage = updateMessage,
                    JiraUrl = $"{Environment.GetEnvironmentVariable("JIRA_BASE_URL")}/browse/{ticketKey}",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ JIRA update test failed: {ex.Message}");
                return BadRequest(new
                {
                    Success = false,
                    Error = ex.Message,
                    Details = ex.ToString(),
                    TicketKey = ticketKey,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
