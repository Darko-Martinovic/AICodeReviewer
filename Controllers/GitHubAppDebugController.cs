using Microsoft.AspNetCore.Mvc;
using AICodeReviewer.Services.Interfaces;

namespace AICodeReviewer.Controllers
{
    /// <summary>
    /// API controller for debugging GitHub App integration
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GitHubAppDebugController : ControllerBase
    {
        private readonly IGitHubAppService _gitHubAppService;
        private readonly IConfigurationService _configurationService;

        public GitHubAppDebugController(IGitHubAppService gitHubAppService, IConfigurationService configurationService)
        {
            _gitHubAppService = gitHubAppService;
            _configurationService = configurationService;
        }

        /// <summary>
        /// Debug GitHub App configuration and authentication
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfiguration()
        {
            try
            {
                var settings = _configurationService.Settings.GitHub.GitHubApp;
                var privateKeyPath = settings.PrivateKeyPath ?? "";
                var privateKeyExists = !string.IsNullOrEmpty(privateKeyPath) && System.IO.File.Exists(privateKeyPath);
                var fullPrivateKeyPath = !string.IsNullOrEmpty(privateKeyPath) ? System.IO.Path.GetFullPath(privateKeyPath) : "N/A";

                return Ok(new
                {
                    GitHubApp = new
                    {
                        settings.AppId,
                        settings.ClientId,
                        settings.InstallationId,
                        PrivateKeyPath = privateKeyPath,
                        FullPrivateKeyPath = fullPrivateKeyPath,
                        PrivateKeyExists = privateKeyExists,
                        settings.UseAppAuthentication,
                        IsAppAuthEnabled = _gitHubAppService.IsAppAuthenticationEnabled
                    },
                    CurrentDirectory = Directory.GetCurrentDirectory(),
                    DebugLogging = _configurationService.Settings.DebugLogging,
                    AllGitHubSettings = new
                    {
                        MaxCommitsToList = _configurationService.Settings.GitHub.MaxCommitsToList,
                        MaxPullRequestsToList = _configurationService.Settings.GitHub.MaxPullRequestsToList,
                        HasGitHubApp = _configurationService.Settings.GitHub.GitHubApp != null
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Test GitHub App authentication
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestAuthentication()
        {
            try
            {
                Console.WriteLine("🧪 Testing GitHub App authentication...");

                var isValid = await _gitHubAppService.ValidateAppConfigurationAsync();

                if (isValid)
                {
                    var repos = await _gitHubAppService.GetInstallationRepositoriesAsync();
                    return Ok(new
                    {
                        IsValid = true,
                        RepositoryCount = repos.Count,
                        Repositories = repos.Take(5).Select(r => new
                        {
                            r.Name,
                            r.FullName,
                            r.Private,
                            r.Owner.Login
                        })
                    });
                }
                else
                {
                    return Ok(new { IsValid = false, Message = "GitHub App authentication failed validation" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GitHub App test failed: {ex.Message}");
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Get raw JWT token for analysis
        /// </summary>
        [HttpGet("jwt")]
        public IActionResult GetJwtToken()
        {
            try
            {
                Console.WriteLine("🔍 Generating JWT token for analysis...");

                if (!_gitHubAppService.IsAppAuthenticationEnabled)
                {
                    return BadRequest(new { Error = "GitHub App authentication is not enabled" });
                }

                // Generate JWT token for debugging
                var jwt = _gitHubAppService.GenerateJwtTokenForDebugging();

                // Parse the JWT to analyze its structure
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var decodedToken = handler.ReadJwtToken(jwt);

                var claims = decodedToken.Claims.ToDictionary(c => c.Type, c => c.Value);

                return Ok(new
                {
                    RawToken = jwt,
                    TokenLength = jwt.Length,
                    Header = decodedToken.Header,
                    Claims = claims,
                    ValidFrom = decodedToken.ValidFrom.ToString("u"),
                    ValidTo = decodedToken.ValidTo.ToString("u"),
                    IssuedAt = decodedToken.IssuedAt.ToString("u"),
                    Issuer = decodedToken.Issuer,
                    Algorithm = decodedToken.SignatureAlgorithm,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ JWT analysis failed: {ex.Message}");
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Check system time and timezone information
        /// </summary>
        [HttpGet("check-time")]
        public IActionResult CheckSystemTime()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var localNow = DateTimeOffset.Now;

            return Ok(new
            {
                UtcTime = utcNow.ToString("u"),
                UtcUnix = utcNow.ToUnixTimeSeconds(),
                LocalTime = localNow.ToString("u"),
                TimeZone = TimeZoneInfo.Local.DisplayName,
                SystemInfo = new
                {
                    MachineName = Environment.MachineName,
                    OsVersion = Environment.OSVersion.ToString(),
                    TickCount = Environment.TickCount64,
                    ProcessorCount = Environment.ProcessorCount
                }
            });
        }

        /// <summary>
        /// Get GitHub server time for synchronization
        /// </summary>
        [HttpGet("github-time")]
        public async Task<IActionResult> GetGitHubServerTime()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "AICodeReviewer/1.0");

                var response = await httpClient.GetAsync("https://api.github.com/zen");
                var dateHeader = response.Headers.Date;

                var systemTime = DateTimeOffset.UtcNow;
                var githubTime = dateHeader ?? DateTimeOffset.UtcNow;
                var timeDiff = (githubTime - systemTime).TotalSeconds;

                return Ok(new
                {
                    SystemTime = systemTime.ToString("u"),
                    SystemUnix = systemTime.ToUnixTimeSeconds(),
                    GitHubTime = githubTime.ToString("u"),
                    GitHubUnix = githubTime.ToUnixTimeSeconds(),
                    TimeDifferenceSeconds = timeDiff,
                    SyncStatus = Math.Abs(timeDiff) < 30 ? "Good" : "Poor"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}