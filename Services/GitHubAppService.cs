using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using Octokit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for handling GitHub App authentication and operations
    /// </summary>
    public class GitHubAppService : IGitHubAppService, IDisposable
    {
        private readonly GitHubAppSettings _appSettings;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<GitHubAppService>? _logger;
        private readonly string _privateKeyPath;
        private readonly RSA? _rsa;  // Cached RSA key
        private bool _disposed = false;

        public GitHubAppService(
            GitHubAppSettings appSettings,
            IConfigurationService configurationService,
            ILogger<GitHubAppService>? logger = null)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger;

            // Set up private key path
            _privateKeyPath = !string.IsNullOrEmpty(_appSettings.PrivateKeyPath)
                ? _appSettings.PrivateKeyPath
                : Path.Combine(Directory.GetCurrentDirectory(), "secrets", "github-app-private-key.pem");

            // Load and cache RSA key during initialization
            if (File.Exists(_privateKeyPath))
            {
                try
                {
                    var privateKeyContent = File.ReadAllText(_privateKeyPath);
                    _rsa = RSA.Create();
                    _rsa.ImportFromPem(privateKeyContent);

                    if (_configurationService.Settings.DebugLogging)
                    {
                        Console.WriteLine($"🔍 GitHub App Service initialized:");
                        Console.WriteLine($"   App ID: {_appSettings.AppId}");
                        Console.WriteLine($"   Installation ID: {_appSettings.InstallationId}");
                        Console.WriteLine($"   RSA Key Size: {_rsa.KeySize}");
                        Console.WriteLine($"   Private Key Path: {_privateKeyPath}");
                        Console.WriteLine($"   Use App Auth: {_appSettings.UseAppAuthentication}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to load RSA private key: {ex.Message}");
                    _logger?.LogError(ex, "Failed to load RSA private key during initialization");
                }
            }
        }

        /// <summary>
        /// Gets whether GitHub App authentication is configured and enabled
        /// </summary>
        public bool IsAppAuthenticationEnabled =>
            _appSettings.UseAppAuthentication &&
            _appSettings.AppId > 0 &&
            _appSettings.InstallationId > 0 &&
            _rsa != null;

        /// <summary>
        /// Creates an authenticated GitHubClient using GitHub App credentials
        /// </summary>
        public async Task<GitHubClient> CreateAppAuthenticatedClientAsync()
        {
            if (!IsAppAuthenticationEnabled)
            {
                throw new InvalidOperationException("GitHub App authentication is not properly configured");
            }

            try
            {
                // Generate JWT token for GitHub App
                var jwt = GenerateJwtToken();

                // Create client with JWT for app authentication
                var appClient = new GitHubClient(new ProductHeaderValue("AICodeReviewer-App"));
                appClient.Credentials = new Credentials(jwt, AuthenticationType.Bearer);

                // Get installation access token
                var response = await appClient.GitHubApps.CreateInstallationToken(_appSettings.InstallationId);

                // Create new client with installation token
                var installationClient = new GitHubClient(new ProductHeaderValue("AICodeReviewer-Installation"));
                installationClient.Credentials = new Credentials(response.Token);

                if (_configurationService.Settings.DebugLogging)
                {
                    Console.WriteLine($"✅ GitHub App authentication successful");
                    Console.WriteLine($"   Token expires: {response.ExpiresAt}");
                }

                return installationClient;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create GitHub App authenticated client");
                Console.WriteLine($"❌ GitHub App authentication failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets repositories accessible to the GitHub App installation
        /// </summary>
        public async Task<IReadOnlyList<Repository>> GetInstallationRepositoriesAsync()
        {
            try
            {
                var client = await CreateAppAuthenticatedClientAsync();
                var repositories = await client.GitHubApps.Installation.GetAllRepositoriesForCurrent();

                if (_configurationService.Settings.DebugLogging)
                {
                    Console.WriteLine($"📚 Found {repositories.TotalCount} repositories for installation");
                }

                return repositories.Repositories;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get installation repositories");
                Console.WriteLine($"❌ Failed to get installation repositories: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates GitHub App configuration and connectivity
        /// </summary>
        public async Task<bool> ValidateAppConfigurationAsync()
        {
            try
            {
                if (!IsAppAuthenticationEnabled)
                {
                    Console.WriteLine($"❌ GitHub App configuration incomplete:");
                    Console.WriteLine($"   Use App Auth: {_appSettings.UseAppAuthentication}");
                    Console.WriteLine($"   App ID: {_appSettings.AppId}");
                    Console.WriteLine($"   Installation ID: {_appSettings.InstallationId}");
                    Console.WriteLine($"   RSA Key Loaded: {_rsa != null}");
                    return false;
                }

                // Test authentication
                var client = await CreateAppAuthenticatedClientAsync();
                var installations = await client.GitHubApps.GetAllInstallationsForCurrent();
                var installation = installations.FirstOrDefault(i => i.Id == _appSettings.InstallationId);

                if (installation != null)
                {
                    Console.WriteLine($"✅ GitHub App validation successful:");
                    Console.WriteLine($"   Installation ID: {installation.Id}");
                    Console.WriteLine($"   Account: {installation.Account.Login}");
                    Console.WriteLine($"   App ID: {installation.AppId}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Installation {_appSettings.InstallationId} not found");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GitHub App validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets GitHub App installation information
        /// </summary>
        public async Task<Installation> GetInstallationAsync()
        {
            var client = await CreateAppAuthenticatedClientAsync();
            var installations = await client.GitHubApps.GetAllInstallationsForCurrent();
            var installation = installations.FirstOrDefault(i => i.Id == _appSettings.InstallationId);
            return installation ?? throw new InvalidOperationException($"Installation {_appSettings.InstallationId} not found");
        }

        /// <summary>
        /// Generates a JWT token for debugging (public wrapper)
        /// </summary>
        public string GenerateJwtTokenForDebugging()
        {
            return GenerateJwtToken();
        }

        /// <summary>
        /// Generates a JWT token for GitHub App authentication using cached RSA key
        /// </summary>
        private string GenerateJwtToken()
        {
            if (_rsa == null)
            {
                throw new InvalidOperationException("RSA private key not loaded");
            }

            try
            {
                var now = DateTimeOffset.UtcNow;
                var iat = now.ToUnixTimeSeconds();
                var exp = now.AddMinutes(9).ToUnixTimeSeconds();

                if (_configurationService.Settings.DebugLogging)
                {
                    Console.WriteLine($"🔑 Generating JWT token...");
                    Console.WriteLine($"🔑 Current UTC time: {now:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"🔑 iat: {iat}, exp: {exp}");
                    Console.WriteLine($"🔑 App ID (iss): {_appSettings.AppId}");
                }

                var signingCredentials = new SigningCredentials(
                    new RsaSecurityKey(_rsa),  // Use cached RSA key
                    SecurityAlgorithms.RsaSha256
                );

                var header = new JwtHeader(signingCredentials);
                var payload = new JwtPayload
                {
                    { "iss", _appSettings.AppId.ToString() },
                    { "iat", iat },
                    { "exp", exp }
                };

                var jwtToken = new JwtSecurityToken(header, payload);
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenString = tokenHandler.WriteToken(jwtToken);

                if (_configurationService.Settings.DebugLogging)
                {
                    Console.WriteLine($"✅ JWT generated successfully, length: {tokenString.Length}");
                }

                return tokenString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ JWT generation failed: {ex.Message}");
                _logger?.LogError(ex, "Failed to generate JWT token for GitHub App");
                throw new InvalidOperationException($"Failed to generate JWT token: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _rsa?.Dispose();
                _disposed = true;
            }
        }
    }
}