using AICodeReviewer.Services.Interfaces;
using Octokit;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for handling GitHub API interactions
    /// </summary>
    public class GitHubService : IGitHubService
    {
        private readonly GitHubClient _gitHubClient;
        private readonly IConfigurationService _configurationService;
        private string _repoOwner;
        private string _repoName;

        public GitHubService(string token, string repoOwner, string repoName, IConfigurationService configurationService)
        {
            _repoOwner = repoOwner ?? throw new ArgumentNullException(nameof(repoOwner));
            _repoName = repoName ?? throw new ArgumentNullException(nameof(repoName));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            _gitHubClient = new GitHubClient(new ProductHeaderValue("AICodeReviewer"));
            _gitHubClient.Credentials = new Credentials(token);
        }

        /// <summary>
        /// Updates the current repository
        /// </summary>
        public void UpdateRepository(string owner, string name)
        {
            _repoOwner = owner ?? throw new ArgumentNullException(nameof(owner));
            _repoName = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Initializes and tests the GitHub connection
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                var user = await _gitHubClient.User.Current();
                Console.WriteLine($"✅ Connected to GitHub as: {user.Login}");

                if (_configurationService.Settings.DebugLogging)
                {
                    Console.WriteLine($"🔍 Attempting to access repository: {_repoOwner}/{_repoName}");
                }

                var repo = await _gitHubClient.Repository.Get(_repoOwner, _repoName);
                Console.WriteLine($"✅ Repository access: {repo.FullName}");
                Console.WriteLine(
                    $"📝 Repository description: {repo.Description ?? "No description"}"
                );
                Console.WriteLine($"🔒 Private repository: {repo.Private}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Repository access failed: {ex.Message}");
                Console.WriteLine($"🔍 Trying to list your repositories...");

                try
                {
                    var currentUser = await _gitHubClient.User.Current();
                    var repos = await _gitHubClient.Repository.GetAllForCurrent();
                    Console.WriteLine($"📚 Found {repos.Count} repositories in your account:");

                    foreach (var r in repos.Take(10))
                    {
                        Console.WriteLine($"  - {r.Name} ({(r.Private ? "private" : "public")})");
                    }

                    Console.WriteLine(
                        $"\n💡 Make sure your .env file has the correct repository name:"
                    );
                    Console.WriteLine($"   GITHUB_REPO_OWNER={currentUser.Login}");
                    Console.WriteLine($"   GITHUB_REPO_NAME=<exact-repository-name>");
                    Console.WriteLine();
                }
                catch (Exception listEx)
                {
                    Console.WriteLine($"❌ Could not list repositories: {listEx.Message}");
                }

                throw;
            }
        }

        /// <summary>
        /// Gets the default branch name for the current repository
        /// </summary>
        private async Task<string> GetDefaultBranchAsync()
        {
            try
            {
                var repo = await _gitHubClient.Repository.Get(_repoOwner, _repoName);
                return repo.DefaultBranch;
            }
            catch
            {
                // Fallback: try to detect if master or main exists
                try
                {
                    // Try master first (common in older repos)
                    await _gitHubClient.Repository.Branch.Get(_repoOwner, _repoName, "master");
                    return "master";
                }
                catch
                {
                    // Default to main
                    return "main";
                }
            }
        }

        /// <summary>
        /// Gets all commits for the default branch
        /// </summary>
        public async Task<IReadOnlyList<GitHubCommit>> GetCommitsAsync()
        {
            var defaultBranch = await GetDefaultBranchAsync();
            return await _gitHubClient.Repository.Commit.GetAll(
                _repoOwner,
                _repoName,
                new CommitRequest { Sha = defaultBranch }
            );
        }

        /// <summary>
        /// Gets recent commits with a specified count
        /// </summary>
        public async Task<IReadOnlyList<GitHubCommit>> GetRecentCommitsAsync(int count = 10)
        {
            var defaultBranch = await GetDefaultBranchAsync();
            var commitRequest = new CommitRequest
            {
                Sha = defaultBranch
            };
            var commits = await _gitHubClient.Repository.Commit.GetAll(_repoOwner, _repoName, commitRequest);
            return commits.Take(count).ToList();
        }

        /// <summary>
        /// Gets detailed commit information including file changes
        /// </summary>
        public async Task<GitHubCommit> GetCommitDetailAsync(string sha)
        {
            return await _gitHubClient.Repository.Commit.Get(_repoOwner, _repoName, sha);
        }

        /// <summary>
        /// Alias for GetCommitDetailAsync to maintain API consistency
        /// </summary>
        public async Task<GitHubCommit> GetCommitAsync(string sha)
        {
            return await GetCommitDetailAsync(sha);
        }

        /// <summary>
        /// Gets all open pull requests for the repository
        /// </summary>
        public async Task<IReadOnlyList<PullRequest>> GetOpenPullRequestsAsync()
        {
            return await _gitHubClient.PullRequest.GetAllForRepository(
                _repoOwner,
                _repoName,
                new PullRequestRequest { State = ItemStateFilter.Open }
            );
        }

        /// <summary>
        /// Gets a specific pull request by number
        /// </summary>
        public async Task<PullRequest> GetPullRequestAsync(int number)
        {
            return await _gitHubClient.PullRequest.Get(_repoOwner, _repoName, number);
        }

        /// <summary>
        /// Gets files changed in a pull request
        /// </summary>
        public async Task<IReadOnlyList<PullRequestFile>> GetPullRequestFilesAsync(int prNumber)
        {
            return await _gitHubClient.PullRequest.Files(_repoOwner, _repoName, prNumber);
        }

        /// <summary>
        /// Gets the content of a file from the repository
        /// </summary>
        public async Task<string> GetFileContentAsync(string fileName)
        {
            try
            {
                var fileContent = await _gitHubClient.Repository.Content.GetAllContents(
                    _repoOwner,
                    _repoName,
                    fileName
                );
                return fileContent.Any() ? fileContent[0].Content : "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Posts a comment to a pull request
        /// </summary>
        public async Task<bool> PostPullRequestCommentAsync(int prNumber, string comment)
        {
            try
            {
                await _gitHubClient.Issue.Comment.Create(_repoOwner, _repoName, prNumber, comment);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Failed to post PR comment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets repository details
        /// </summary>
        public async Task<Repository> GetRepositoryDetailsAsync()
        {
            return await _gitHubClient.Repository.Get(_repoOwner, _repoName);
        }

        /// <summary>
        /// Gets repository info as tuple
        /// </summary>
        public Task<(string Owner, string Name)> GetRepositoryInfoAsync()
        {
            return Task.FromResult((_repoOwner, _repoName));
        }

        /// <summary>
        /// Gets open pull requests
        /// </summary>
        public async Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync()
        {
            var request = new PullRequestRequest()
            {
                State = ItemStateFilter.Open
            };
            return await _gitHubClient.PullRequest.GetAllForRepository(_repoOwner, _repoName, request);
        }
    }
}
