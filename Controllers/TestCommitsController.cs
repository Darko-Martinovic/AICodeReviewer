using Microsoft.AspNetCore.Mvc;

namespace AICodeReviewer.Controllers
{
    /// <summary>
    /// Minimal test controller to debug commits endpoint issues
    /// </summary>
    [ApiController]
    [Route("api/testcommits")]
    [Produces("application/json")]
    public class TestCommitsController : ControllerBase
    {
        public TestCommitsController()
        {
        }

        /// <summary>
        /// Test commits endpoint with no dependencies
        /// </summary>
        [HttpGet("recent")]
        public IActionResult GetRecentCommits([FromQuery] int count = 10)
        {
            try
            {
                // Return the structure the frontend expects
                return Ok(new
                {
                    Repository = "Darko-Martinovic/TestRepo",
                    Count = count,
                    commits = new[]
                    {
                        new
                        {
                            sha = "abc123test",
                            message = "Test commit from TestCommitsController",
                            author = "Test Author",
                            authorEmail = "test@example.com",
                            date = DateTime.UtcNow.AddHours(-1),
                            htmlUrl = "https://github.com/test/test/commit/abc123test"
                        },
                        new
                        {
                            sha = "def456test",
                            message = "Another test commit",
                            author = "Test Author 2",
                            authorEmail = "test2@example.com",
                            date = DateTime.UtcNow.AddHours(-2),
                            htmlUrl = "https://github.com/test/test/commit/def456test"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Another test endpoint
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { Message = "Test endpoint working", Timestamp = DateTime.UtcNow });
        }
    }
}
