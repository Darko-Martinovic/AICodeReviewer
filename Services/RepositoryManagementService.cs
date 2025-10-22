using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Services.Interfaces;
using Octokit;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for managing repository selection and history
    /// </summary>
    public class RepositoryManagementService : IRepositoryManagementService
    {
        private readonly AppSettings _settings;
        private readonly GitHubClient _gitHubClient;
        private readonly IRepositoryFilterService _filterService;
        private (string Owner, string Name) _currentRepository;

        public RepositoryManagementService(AppSettings settings, string gitHubToken, IRepositoryFilterService filterService)
        {
            _settings = settings;
            _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            _gitHubClient = new GitHubClient(new ProductHeaderValue("AICodeReviewer"));
            _gitHubClient.Credentials = new Credentials(gitHubToken);

            // Initialize with default repository
            var defaultOwner = _settings.GitHub.DefaultRepository?.Owner;
            var defaultName = _settings.GitHub.DefaultRepository?.Name;

            // Use fallback values if configuration is missing
            _currentRepository = (
                !string.IsNullOrEmpty(defaultOwner) ? defaultOwner : "Darko-Martinovic",
                !string.IsNullOrEmpty(defaultName) ? defaultName : "AICodeReviewer"
            );

            if (_settings.DebugLogging)
            {
                Console.WriteLine($"🔍 Debug: Initialized with repository: {_currentRepository.Owner}/{_currentRepository.Name}");
            }
        }

        /// <summary>
        /// Gets the current repository information
        /// </summary>
        public Task<(string Owner, string Name)> GetCurrentRepositoryAsync()
        {
            return Task.FromResult(_currentRepository);
        }

        /// <summary>
        /// Sets the current repository (for web API calls)
        /// </summary>
        /// <param name="owner">Repository owner</param>
        /// <param name="name">Repository name</param>
        public async Task SetCurrentRepositoryAsync(string owner, string name)
        {
            _currentRepository = (owner, name);

            if (_settings.DebugLogging)
            {
                Console.WriteLine($"🔍 Debug: Current repository changed to: {owner}/{name}");
            }

            // Add to history
            await AddToHistoryAsync(owner, name);
        }

        /// <summary>
        /// Prompts user to select a repository and returns the selection
        /// </summary>
        public async Task<(string Owner, string Name)> PromptForRepositoryAsync()
        {
            Console.WriteLine("🏠 Repository Selection");
            Console.WriteLine("──────────────────────────────────────────────────────────────");

            // Show current repository
            Console.WriteLine($"📍 Current repository: {_currentRepository.Owner}/{_currentRepository.Name}");
            Console.WriteLine();

            // Get repository history
            var history = await GetRepositoryHistoryAsync();

            if (history.Any())
            {
                Console.WriteLine("📚 Recent repositories:");
                for (int i = 0; i < history.Count; i++)
                {
                    var repo = history[i];
                    var currentIndicator = (repo.Owner == _currentRepository.Owner && repo.Name == _currentRepository.Name) ? "📍 " : "   ";
                    Console.WriteLine($"{currentIndicator}{i + 1}. {repo.FullName}");
                    if (!string.IsNullOrEmpty(repo.Description))
                    {
                        Console.WriteLine($"     {repo.Description}");
                    }
                }
                Console.WriteLine();
            }

            Console.WriteLine("Options:");
            Console.WriteLine("  - Press Enter to use current repository");
            Console.WriteLine("  - Enter a number to select from history");
            Console.WriteLine("  - Enter 'list' to see all your repositories");
            Console.WriteLine("  - Enter 'new' to specify a new repository (not in your account)");
            Console.WriteLine();

            Console.Write("Enter your choice: ");
            var choice = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(choice))
            {
                // Use current repository
                Console.WriteLine($"✅ Using current repository: {_currentRepository.Owner}/{_currentRepository.Name}");
                return _currentRepository;
            }

            if (choice.ToLower() == "new")
            {
                return await PromptForNewRepositoryAsync();
            }

            if (choice.ToLower() == "list")
            {
                return await PromptFromAvailableRepositoriesAsync();
            }

            // Try to parse as number for history selection
            if (int.TryParse(choice, out int selection) && selection >= 1 && selection <= history.Count)
            {
                var selectedRepo = history[selection - 1];
                _currentRepository = (selectedRepo.Owner, selectedRepo.Name);
                Console.WriteLine($"✅ Selected repository: {selectedRepo.FullName}");
                return _currentRepository;
            }

            // Try to parse as "owner/repo" format
            if (choice.Contains('/'))
            {
                var parts = choice.Split('/', 2);
                if (parts.Length == 2)
                {
                    var owner = parts[0].Trim();
                    var name = parts[1].Trim();

                    if (await ValidateRepositoryAsync(owner, name))
                    {
                        _currentRepository = (owner, name);
                        await AddToHistoryAsync(owner, name);
                        Console.WriteLine($"✅ Selected repository: {owner}/{name}");
                        return _currentRepository;
                    }
                    else
                    {
                        Console.WriteLine("❌ Repository not found or not accessible. Please try again.");
                        return await PromptForRepositoryAsync();
                    }
                }
            }

            Console.WriteLine("❌ Invalid choice. Please try again.");
            return await PromptForRepositoryAsync();
        }

        /// <summary>
        /// Prompts user to enter a new repository
        /// </summary>
        private async Task<(string Owner, string Name)> PromptForNewRepositoryAsync()
        {
            Console.WriteLine("🔍 Enter new repository details:");
            Console.WriteLine("💡 This is for repositories not in your account (e.g., other users' public repos)");
            Console.WriteLine();

            Console.Write("Repository owner (username/organization): ");
            var owner = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(owner))
            {
                Console.WriteLine("❌ Owner cannot be empty.");
                return await PromptForRepositoryAsync();
            }

            Console.Write("Repository name: ");
            var name = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("❌ Repository name cannot be empty.");
                return await PromptForRepositoryAsync();
            }

            Console.WriteLine($"🔍 Validating repository: {owner}/{name}...");
            if (await ValidateRepositoryAsync(owner, name))
            {
                _currentRepository = (owner, name);
                await AddToHistoryAsync(owner, name);
                Console.WriteLine($"✅ Repository validated: {owner}/{name}");
                return _currentRepository;
            }
            else
            {
                Console.WriteLine("❌ Repository not found or not accessible. Please try again.");
                return await PromptForRepositoryAsync();
            }
        }

        /// <summary>
        /// Prompts user to select from available repositories
        /// </summary>
        private async Task<(string Owner, string Name)> PromptFromAvailableRepositoriesAsync()
        {
            Console.WriteLine("📚 Fetching your repositories...");
            var availableRepos = await GetAvailableRepositoriesAsync();

            if (!availableRepos.Any())
            {
                Console.WriteLine("❌ No repositories found or accessible.");
                return await PromptForRepositoryAsync();
            }

            Console.WriteLine($"\n📚 Available repositories ({availableRepos.Count} found):");
            for (int i = 0; i < availableRepos.Count; i++)
            {
                var repo = availableRepos[i];
                var currentIndicator = (repo.Owner == _currentRepository.Owner && repo.Name == _currentRepository.Name) ? "📍 " : "   ";
                Console.WriteLine($"{currentIndicator}{i + 1}. {repo.FullName} ({(repo.IsPrivate ? "private" : "public")})");
                if (!string.IsNullOrEmpty(repo.Description))
                {
                    Console.WriteLine($"     {repo.Description}");
                }
            }

            Console.WriteLine("\n💡 Select a repository to switch to it:");
            Console.Write($"Enter selection (1-{availableRepos.Count}) or 0 to cancel: ");
            if (int.TryParse(Console.ReadLine(), out int selection))
            {
                if (selection == 0)
                {
                    Console.WriteLine("❌ Cancelled.");
                    return await PromptForRepositoryAsync();
                }

                if (selection >= 1 && selection <= availableRepos.Count)
                {
                    var selectedRepo = availableRepos[selection - 1];
                    _currentRepository = (selectedRepo.Owner, selectedRepo.Name);
                    await AddToHistoryAsync(selectedRepo.Owner, selectedRepo.Name, selectedRepo.Description);
                    Console.WriteLine($"✅ Selected repository: {selectedRepo.FullName}");
                    return _currentRepository;
                }
                else
                {
                    Console.WriteLine($"❌ Invalid selection. Please enter a number between 1 and {availableRepos.Count}.");
                    return await PromptFromAvailableRepositoriesAsync();
                }
            }
            else
            {
                Console.WriteLine("❌ Invalid input. Please enter a number.");
                return await PromptFromAvailableRepositoriesAsync();
            }
        }

        /// <summary>
        /// Gets the list of recently used repositories
        /// </summary>
        public async Task<List<RepositoryInfo>> GetRepositoryHistoryAsync()
        {
            var history = _settings.GitHub.RepositoryHistory ?? new List<RepositoryInfo>();

            // Add current repository if not in history
            if (!history.Any(r => r.Owner == _currentRepository.Owner && r.Name == _currentRepository.Name))
            {
                history.Insert(0, new RepositoryInfo
                {
                    Owner = _currentRepository.Owner,
                    Name = _currentRepository.Name,
                    Description = "Current repository"
                });
            }

            return history.Take(10).ToList(); // Limit to 10 most recent
        }

        /// <summary>
        /// Adds a repository to the history
        /// </summary>
        public async Task AddToHistoryAsync(string owner, string name, string description = "")
        {
            var history = await GetRepositoryHistoryAsync();

            // Remove if already exists
            history.RemoveAll(r => r.Owner == owner && r.Name == name);

            // Add to beginning
            history.Insert(0, new RepositoryInfo
            {
                Owner = owner,
                Name = name,
                Description = description
            });

            // Keep only the most recent 10
            history = history.Take(10).ToList();

            // Note: In a real implementation, you'd want to persist this to a file or database
            // For now, we'll just update the in-memory list
            Console.WriteLine($"📝 Added {owner}/{name} to repository history");
        }

        /// <summary>
        /// Validates if a repository exists and is accessible
        /// </summary>
        public async Task<bool> ValidateRepositoryAsync(string owner, string name)
        {
            try
            {
                var repo = await _gitHubClient.Repository.Get(owner, name);
                return repo != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets available repositories for the current user
        /// </summary>
        public async Task<List<RepositoryInfo>> GetAvailableRepositoriesAsync()
        {
            try
            {
                var repos = await _gitHubClient.Repository.GetAllForCurrent();
                var repositoryList = repos.Select(r => new RepositoryInfo
                {
                    Owner = r.Owner.Login,
                    Name = r.Name,
                    Description = r.Description ?? "",
                    IsPrivate = r.Private,
                    DefaultBranch = r.DefaultBranch ?? "main",
                    StarCount = r.StargazersCount,
                    ForkCount = r.ForksCount,
                    Language = r.Language ?? ""
                }).ToList();

                // Apply filtering
                var filteredRepositories = _filterService.FilterRepositories(repositoryList);

                return filteredRepositories;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Could not fetch repositories: {ex.Message}");
                return new List<RepositoryInfo>();
            }
        }
    }
}