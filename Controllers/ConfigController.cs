using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Asp.Versioning;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
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

        /// <summary>
        /// Get Azure OpenAI configuration settings
        /// </summary>
        [HttpGet("azure-openai")]
        public IActionResult GetAzureOpenAIConfig()
        {
            try
            {
                // Check if configuration service is available
                if (_configurationService?.Settings?.AzureOpenAI == null)
                {
                    return Ok(new
                    {
                        endpoint = "Not configured",
                        apiKey = "***",
                        deploymentName = "Not configured",
                        apiVersion = "2024-02-01",
                        temperature = 0.1f,
                        maxTokens = 4000,
                        contentLimit = 15000,
                        message = "Using default configuration - Development settings may not be loaded"
                    });
                }

                var settings = _configurationService.Settings.AzureOpenAI;
                
                return Ok(new
                {
                    endpoint = "See appsettings.json",
                    apiKey = "***", // Never expose the actual API key
                    deploymentName = "See appsettings.json",
                    apiVersion = settings.ApiVersion,
                    temperature = settings.Temperature,
                    maxTokens = settings.MaxTokens,
                    contentLimit = settings.ContentLimit
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Update Azure OpenAI runtime settings (Temperature, MaxTokens, ContentLimit)
        /// Note: Endpoint, ApiKey, DeploymentName, and ApiVersion require app restart
        /// </summary>
        [HttpPost("azure-openai")]
        public IActionResult UpdateAzureOpenAIConfig([FromBody] UpdateAzureOpenAIRequest request)
        {
            try
            {
                if (_configurationService?.Settings?.AzureOpenAI == null)
                {
                    return BadRequest(new { error = "Configuration service not available" });
                }

                var settings = _configurationService.Settings.AzureOpenAI;

                // Update runtime settings
                if (request.Temperature.HasValue)
                {
                    if (request.Temperature.Value < 0 || request.Temperature.Value > 2)
                    {
                        return BadRequest(new { error = "Temperature must be between 0 and 2" });
                    }
                    settings.Temperature = request.Temperature.Value;
                }

                if (request.MaxTokens.HasValue)
                {
                    if (request.MaxTokens.Value < 1 || request.MaxTokens.Value > 128000)
                    {
                        return BadRequest(new { error = "MaxTokens must be between 1 and 128000" });
                    }
                    settings.MaxTokens = request.MaxTokens.Value;
                }

                if (request.ContentLimit.HasValue)
                {
                    if (request.ContentLimit.Value < 1000)
                    {
                        return BadRequest(new { error = "ContentLimit must be at least 1000" });
                    }
                    settings.ContentLimit = request.ContentLimit.Value;
                }

                return Ok(new
                {
                    message = "Runtime settings updated successfully",
                    temperature = settings.Temperature,
                    maxTokens = settings.MaxTokens,
                    contentLimit = settings.ContentLimit,
                    note = "Endpoint, ApiKey, DeploymentName, and ApiVersion changes require app restart"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }

    public class UpdateAzureOpenAIRequest
    {
        public float? Temperature { get; set; }
        public int? MaxTokens { get; set; }
        public int? ContentLimit { get; set; }
    }
}
