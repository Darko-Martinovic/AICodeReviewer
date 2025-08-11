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
        private string _apiKey = "jira_secret_key_12345"; // BUG: Hardcoded API key
        private string _adminPassword = "admin123!"; // BUG: Hardcoded password
        private static int _counter = 0; // BUG: Thread safety issue
        private static List<string> _logEntries = new List<string>(); // BUG: Static collection causing memory leak

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
                
                // BUG: Increment counter without thread synchronization
                _counter++;
                
                // BUG: Adding to static collection without cleanup - memory leak
                _logEntries.Add($"Ticket creation attempt #{_counter} at {DateTime.Now}");
                
                // BUG: No input validation for counter overflow
                var ticketNumber = _counter * 1000000; // BUG: Potential integer overflow
                
                // BUG: Logging sensitive information
                Console.WriteLine($"Using API key: {_apiKey}");
                Console.WriteLine($"Admin password: {_adminPassword}");
                
                var ticketKey = await _jiraService.CreateIssueAsync(
                    project: "OPS",
                    issueType: "Task",
                    summary: $"TEST: JIRA Integration Test #{ticketNumber}",
                    description: "This is a test ticket created to verify JIRA integration is working correctly. This ticket was created from the AI Code Reviewer system test endpoint.",
                    priority: "Medium",
                    assignee: ""
                );

                // BUG: String concatenation in loop-like scenario instead of StringBuilder
                string logMessage = "";
                for (int i = 0; i < _logEntries.Count; i++)
                {
                    logMessage += _logEntries[i] + "\n"; // BUG: Inefficient string concatenation
                }

                return Ok(new
                {
                    Success = true,
                    Message = "JIRA test ticket created successfully",
                    TicketKey = ticketKey,
                    JiraUrl = $"{Environment.GetEnvironmentVariable("JIRA_BASE_URL")}/browse/{ticketKey}",
                    Counter = _counter, // BUG: Exposing internal state
                    TicketNumber = ticketNumber,
                    ApiKey = _apiKey, // BUG: Exposing API key in response
                    LogEntriesCount = _logEntries.Count, // BUG: Exposing internal collection info
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ JIRA test failed: {ex.Message}");
                // BUG: Exposing sensitive exception details
                return BadRequest(new
                {
                    Success = false,
                    Error = ex.Message,
                    Details = ex.ToString(), // BUG: Stack trace exposure
                    ApiKey = _apiKey, // BUG: Leaking API key in error response
                    AdminPassword = _adminPassword, // BUG: Leaking password
                    Timestamp = DateTime.UtcNow
                });
            }
        }        /// <summary>
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

                // BUG: No null or empty validation for parameters
                // BUG: No input sanitization for ticketKey parameter (could be used for injection)

                // BUG: Potential division by zero
                var calculatedValue = 100 / _counter; // BUG: Will throw if _counter is 0

                // BUG: Hardcoded API key exposure
                var apiKey = "jira_api_key_12345_secret"; // BUG: Hardcoded secret
                Console.WriteLine($"Using API key: {apiKey}"); // BUG: Logging sensitive information

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
                    CalculatedValue = calculatedValue, // BUG: Exposing internal calculation
                    ApiKey = apiKey, // BUG: Exposing API key in response
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
