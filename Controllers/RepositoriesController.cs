using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Controllers
{
    /// <summary>
    /// API controller for repository management operations
    /// Mirrors the console menu option 6
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RepositoriesController : ControllerBase
    {
        private readonly IRepositoryManagementService _repositoryService;
        private readonly IGitHubService _gitHubService;

        public RepositoriesController(IRepositoryManagementService repositoryService, IGitHubService gitHubService)
        {
            _repositoryService = repositoryService;
            _gitHubService = gitHubService;
        }

        /// <summary>
        /// Gets the current repository information
        /// </summary>
        /// <returns>Current repository details</returns>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentRepository()
        {
            try
            {
                var (owner, name) = await _repositoryService.GetCurrentRepositoryAsync();

                return Ok(new
                {
                    Owner = owner,
                    Name = name,
                    FullName = $"{owner}/{name}"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets all available repositories (combines history and available)
        /// This is the main endpoint the React app calls
        /// </summary>
        /// <returns>List of all repositories</returns>
        [HttpGet]
        public async Task<IActionResult> GetRepositories()
        {
            try
            {
                var availableRepos = await _repositoryService.GetAvailableRepositoriesAsync();
                var history = await _repositoryService.GetRepositoryHistoryAsync();
                var (currentOwner, currentName) = await _repositoryService.GetCurrentRepositoryAsync();

                // Combine and deduplicate repositories
                var allRepos = new List<object>();

                // Find the current repository in available repos to get its proper description
                var currentRepoInfo = availableRepos.FirstOrDefault(r =>
                    r.Owner == currentOwner && r.Name == currentName);

                // Add current repository first with proper description
                allRepos.Add(new
                {
                    Id = 0,
                    Name = currentName,
                    FullName = $"{currentOwner}/{currentName}",
                    Owner = currentOwner,
                    Description = currentRepoInfo?.Description ?? "Repository information not available",
                    DefaultBranch = currentRepoInfo?.DefaultBranch ?? "master",
                    Private = currentRepoInfo?.IsPrivate ?? false,
                    HtmlUrl = $"https://github.com/{currentOwner}/{currentName}",
                    StarCount = currentRepoInfo?.StarCount ?? 0,
                    ForkCount = currentRepoInfo?.ForkCount ?? 0,
                    IsCurrent = true
                });

                // Add available repositories
                foreach (var repo in availableRepos)
                {
                    if (repo.Owner != currentOwner || repo.Name != currentName) // Don't duplicate current
                    {
                        allRepos.Add(new
                        {
                            Id = allRepos.Count,
                            Name = repo.Name,
                            FullName = repo.FullName,
                            Owner = repo.Owner,
                            Description = repo.Description,
                            DefaultBranch = repo.DefaultBranch,
                            Private = repo.IsPrivate,
                            HtmlUrl = $"https://github.com/{repo.Owner}/{repo.Name}",
                            StarCount = repo.StarCount,
                            ForkCount = repo.ForkCount,
                            IsCurrent = false
                        });
                    }
                }

                return Ok(allRepos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the repository history
        /// </summary>
        /// <returns>List of recently used repositories</returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetRepositoryHistory()
        {
            try
            {
                var history = await _repositoryService.GetRepositoryHistoryAsync();

                return Ok(new
                {
                    Count = history.Count,
                    Repositories = history
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Gets available repositories for the current user
        /// </summary>
        /// <returns>List of available repositories</returns>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableRepositories()
        {
            try
            {
                var repositories = await _repositoryService.GetAvailableRepositoriesAsync();

                return Ok(new
                {
                    Count = repositories.Count,
                    Repositories = repositories
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Validates if a repository exists and is accessible
        /// </summary>
        /// <param name="owner">Repository owner</param>
        /// <param name="name">Repository name</param>
        /// <returns>Validation result</returns>
        [HttpGet("validate/{owner}/{name}")]
        public async Task<IActionResult> ValidateRepository(string owner, string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new { Error = "Owner and name are required" });
                }

                var isValid = await _repositoryService.ValidateRepositoryAsync(owner, name);

                return Ok(new
                {
                    Owner = owner,
                    Name = name,
                    FullName = $"{owner}/{name}",
                    IsValid = isValid
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Sets the current repository (equivalent to repository selection in console)
        /// </summary>
        /// <param name="request">Repository selection request</param>
        /// <returns>Updated repository information</returns>
        [HttpPost("current")]
        public async Task<IActionResult> SetCurrentRepository([FromBody] SetRepositoryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Owner) || string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { Error = "Owner and name are required" });
                }

                // Validate repository first
                var isValid = await _repositoryService.ValidateRepositoryAsync(request.Owner, request.Name);
                if (!isValid)
                {
                    return BadRequest(new { Error = "Repository not found or not accessible" });
                }

                // Set current repository and add to history
                await _repositoryService.SetCurrentRepositoryAsync(request.Owner, request.Name);

                // Update GitHubService to use the new repository
                _gitHubService.UpdateRepository(request.Owner, request.Name);

                return Ok(new
                {
                    Owner = request.Owner,
                    Name = request.Name,
                    FullName = $"{request.Owner}/{request.Name}",
                    Description = request.Description,
                    Message = "Repository updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Sets the current repository (alias for SetCurrentRepository)
        /// This endpoint matches the frontend API call expectation
        /// </summary>
        /// <param name="request">Repository selection request</param>
        /// <returns>Updated repository information</returns>
        [HttpPost("set")]
        public async Task<IActionResult> SetRepository([FromBody] SetRepositoryRequest request)
        {
            // Delegate to the main implementation
            return await SetCurrentRepository(request);
        }
    }

    /// <summary>
    /// Request model for setting current repository
    /// </summary>
    public class SetRepositoryRequest
    {
        public string Owner { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
