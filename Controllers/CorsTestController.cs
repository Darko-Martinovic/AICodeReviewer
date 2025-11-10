using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace AICodeReviewer.Controllers
{
    /// <summary>
    /// Simple controller to test CORS configuration
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowReactApp")]
    public class CorsTestController : ControllerBase
    {
        /// <summary>
        /// Simple test endpoint with explicit CORS headers
        /// </summary>
        [HttpGet]
        [HttpOptions]
        public IActionResult Test()
        {
            // Explicitly add CORS headers for debugging
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            Response.Headers.Append("Access-Control-Allow-Headers", "*");
            
            return Ok(new
            {
                Message = "CORS Test Successful!",
                Timestamp = DateTime.UtcNow,
                Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            });
        }

        /// <summary>
        /// Handle OPTIONS preflight requests explicitly
        /// </summary>
        [HttpOptions]
        public IActionResult Options()
        {
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, PATCH");
            Response.Headers.Append("Access-Control-Allow-Headers", "*");
            Response.Headers.Append("Access-Control-Max-Age", "3600");
            
            return Ok();
        }
    }
}

