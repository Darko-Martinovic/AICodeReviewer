using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
