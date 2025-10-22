using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Models.Configuration;

namespace AICodeReviewer.Controllers
{
    /// <summary>
    /// API controller for managing repository filter settings
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RepositoryFiltersController : ControllerBase
    {
        private readonly IRepositoryFilterService _filterService;
        private readonly IRepositoryManagementService _repositoryService;

        public RepositoryFiltersController(
            IRepositoryFilterService filterService,
            IRepositoryManagementService repositoryService)
        {
            _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        }

        /// <summary>
        /// Gets the current repository filter settings
        /// </summary>
        /// <returns>Current filter configuration</returns>
        [HttpGet]
        public IActionResult GetFilterSettings()
        {
            try
            {
                var settings = _filterService.GetFilterSettings();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Updates the repository filter settings
        /// </summary>
        /// <param name="settings">New filter settings</param>
        /// <returns>Updated settings</returns>
        [HttpPost]
        public async Task<IActionResult> UpdateFilterSettings([FromBody] RepositoryFilterSettings settings)
        {
            try
            {
                if (settings == null)
                {
                    return BadRequest(new { Error = "Filter settings cannot be null" });
                }

                // Validate all patterns
                var validationErrors = new List<string>();
                foreach (var pattern in settings.IncludePatterns.Concat(settings.ExcludePatterns))
                {
                    var (isValid, errorMessage) = _filterService.ValidatePattern(pattern);
                    if (!isValid)
                    {
                        validationErrors.Add($"Pattern '{pattern.Pattern}': {errorMessage}");
                    }
                }

                if (validationErrors.Any())
                {
                    return BadRequest(new
                    {
                        Error = "Invalid filter patterns",
                        Details = validationErrors
                    });
                }

                await _filterService.UpdateFilterSettingsAsync(settings);

                return Ok(new
                {
                    Settings = settings,
                    Message = "Filter settings updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Tests a filter pattern against a repository name
        /// </summary>
        /// <param name="request">Test pattern request</param>
        /// <returns>Test result</returns>
        [HttpPost("test")]
        public IActionResult TestPattern([FromBody] TestPatternRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.RepositoryName))
                {
                    return BadRequest(new { Error = "Repository name is required" });
                }

                var (isValid, errorMessage) = _filterService.ValidatePattern(request.Pattern);
                if (!isValid)
                {
                    return BadRequest(new { Error = errorMessage });
                }

                var matches = _filterService.TestPattern(
                    request.RepositoryName,
                    request.Owner ?? "",
                    request.Pattern);

                return Ok(new
                {
                    RepositoryName = request.RepositoryName,
                    Owner = request.Owner,
                    Pattern = request.Pattern.Pattern,
                    Matches = matches,
                    Message = matches
                        ? $"Repository '{request.Owner}/{request.RepositoryName}' matches the pattern"
                        : $"Repository '{request.Owner}/{request.RepositoryName}' does not match the pattern"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a preview of filtered repositories based on current settings
        /// </summary>
        /// <returns>Filtered repository list</returns>
        [HttpGet("preview")]
        public async Task<IActionResult> GetFilteredRepositories()
        {
            try
            {
                var allRepositories = await _repositoryService.GetAvailableRepositoriesAsync();

                return Ok(new
                {
                    TotalRepositories = allRepositories.Count,
                    FilteredRepositories = allRepositories,
                    FilterSettings = _filterService.GetFilterSettings()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Validates a filter pattern without applying it
        /// </summary>
        /// <param name="pattern">Pattern to validate</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate")]
        public IActionResult ValidatePattern([FromBody] RepositoryFilterPattern pattern)
        {
            try
            {
                var (isValid, errorMessage) = _filterService.ValidatePattern(pattern);

                return Ok(new
                {
                    IsValid = isValid,
                    ErrorMessage = errorMessage,
                    Pattern = pattern.Pattern
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Resets filter settings to default (show all repositories)
        /// </summary>
        /// <returns>Reset confirmation</returns>
        [HttpDelete]
        public async Task<IActionResult> ResetFilterSettings()
        {
            try
            {
                var defaultSettings = new RepositoryFilterSettings
                {
                    EnableFiltering = false,
                    DefaultMode = FilterMode.ShowAll,
                    IncludePatterns = new List<RepositoryFilterPattern>(),
                    ExcludePatterns = new List<RepositoryFilterPattern>()
                };

                await _filterService.UpdateFilterSettingsAsync(defaultSettings);

                return Ok(new
                {
                    Settings = defaultSettings,
                    Message = "Filter settings reset to default"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for testing a filter pattern
    /// </summary>
    public class TestPatternRequest
    {
        public string RepositoryName { get; set; } = string.Empty;
        public string? Owner { get; set; }
        public RepositoryFilterPattern Pattern { get; set; } = new();
    }
}