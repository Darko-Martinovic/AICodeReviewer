using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowReactApp")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;

        public ConfigController(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        /// <summary>
        /// Get code review configuration settings
        /// </summary>
        [HttpGet("code-review")]
        public IActionResult GetCodeReviewConfig()
        {
            try
            {
                // Check if configuration service is available
                if (_configurationService?.Settings?.CodeReview == null)
                {
                    return Ok(new
                    {
                        maxFilesToReview = 10,
                        maxIssuesInSummary = 20,
                        showTokenMetrics = true,
                        message = "Using default configuration - Development settings may not be loaded"
                    });
                }

                var settings = _configurationService.Settings.CodeReview;
                return Ok(new
                {
                    maxFilesToReview = settings.MaxFilesToReview,
                    maxIssuesInSummary = settings.MaxIssuesInSummary,
                    showTokenMetrics = settings.ShowTokenMetrics
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
