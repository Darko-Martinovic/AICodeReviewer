using AICodeReviewer.Services.Interfaces;
using Octokit;
using System.Text;

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
        /// Gets all repository branches
        /// </summary>
        public async Task<IReadOnlyList<string>> GetBranchesAsync()
        {
            var branches = await _gitHubClient.Repository.Branch.GetAll(_repoOwner, _repoName);
            return branches.Select(b => b.Name).ToList();
        }

        /// <summary>
        /// Gets all commits for the specified branch (or default branch if not specified)
        /// </summary>
        public async Task<IReadOnlyList<GitHubCommit>> GetCommitsAsync(string? branch = null)
        {
            var targetBranch = branch ?? await GetDefaultBranchAsync();
            return await _gitHubClient.Repository.Commit.GetAll(
                _repoOwner,
                _repoName,
                new CommitRequest { Sha = targetBranch }
            );
        }

        /// <summary>
        /// Gets recent commits with a specified count for the specified branch (or default branch if not specified)
        /// </summary>
        public async Task<IReadOnlyList<GitHubCommit>> GetRecentCommitsAsync(int count = 10, string? branch = null)
        {
            var targetBranch = branch ?? await GetDefaultBranchAsync();
            var commitRequest = new CommitRequest
            {
                Sha = targetBranch
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
        /// Gets files changed in a pull request with pagination support
        /// </summary>
        public async Task<IReadOnlyList<PullRequestFile>> GetPullRequestFilesAsync(int prNumber)
        {
            try
            {
                // Use ApiOptions to get all files, not just the first page
                var apiOptions = new ApiOptions
                {
                    PageSize = 100, // Maximum page size
                    StartPage = 1,
                    PageCount = 10  // Allow up to 10 pages (1000 files max)
                };

                var allFiles = await _gitHubClient.PullRequest.Files(_repoOwner, _repoName, prNumber, apiOptions);
                Console.WriteLine($"      📁 Retrieved {allFiles.Count} total files from GitHub API");

                return allFiles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Error retrieving PR files: {ex.Message}");
                // Fallback to default method without pagination
                return await _gitHubClient.PullRequest.Files(_repoOwner, _repoName, prNumber);
            }
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
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ GitHub API error for {fileName}: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Gets the content of a file from a specific branch (for PR reviews)
        /// </summary>
        public async Task<string> GetFileContentFromBranchAsync(string fileName, string branch)
        {
            try
            {
                Console.WriteLine($"      🌿 Trying to get {fileName} from branch: {branch}");

                // FIXED: Use GetAllContentsByRef to specify the branch reference
                var fileContent = await _gitHubClient.Repository.Content.GetAllContentsByRef(
                    _repoOwner,
                    _repoName,
                    fileName,
                    branch  // Branch reference
                );
                var content = fileContent.Any() ? fileContent[0].Content : "";
                Console.WriteLine($"      ✅ Retrieved {content.Length} characters");
                return content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Branch API error for {fileName}: {ex.Message}");
                // Try alternative approach - get raw file content from PR branch
                try
                {
                    Console.WriteLine($"      🔄 Trying alternative approach for {fileName}...");
                    var commits = await _gitHubClient.Repository.Commit.GetAll(_repoOwner, _repoName, new CommitRequest { Sha = branch });
                    if (commits.Any())
                    {
                        var latestCommit = commits.First();
                        var tree = await _gitHubClient.Git.Tree.Get(_repoOwner, _repoName, latestCommit.Sha);
                        var fileItem = tree.Tree.FirstOrDefault(t => t.Path == fileName);
                        if (fileItem != null)
                        {
                            var blob = await _gitHubClient.Git.Blob.Get(_repoOwner, _repoName, fileItem.Sha);
                            var content = Encoding.UTF8.GetString(Convert.FromBase64String(blob.Content));
                            Console.WriteLine($"      ✅ Retrieved via Git API: {content.Length} characters");
                            return content;
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"      ❌ Alternative approach failed: {ex2.Message}");
                }
                return "";
            }
        }

        /// <summary>
        /// Gets PR files with branch information 
        /// </summary>
        public async Task<(IReadOnlyList<PullRequestFile> files, string headBranch)> GetPullRequestFilesWithBranchAsync(int prNumber)
        {
            try
            {
                var pr = await _gitHubClient.PullRequest.Get(_repoOwner, _repoName, prNumber);

                // Use pagination to get all files
                var apiOptions = new ApiOptions
                {
                    PageSize = 100,
                    StartPage = 1,
                    PageCount = 10
                };

                var files = await _gitHubClient.PullRequest.Files(_repoOwner, _repoName, prNumber, apiOptions);
                Console.WriteLine($"      🔍 PR #{prNumber} head branch: {pr.Head.Ref}");
                Console.WriteLine($"      📁 Retrieved {files.Count} total files with pagination");
                return (files, pr.Head.Ref);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Error getting PR info: {ex.Message}");
                // Fallback with pagination
                try
                {
                    var apiOptions = new ApiOptions { PageSize = 100, StartPage = 1, PageCount = 10 };
                    var files = await _gitHubClient.PullRequest.Files(_repoOwner, _repoName, prNumber, apiOptions);
                    return (files, "main");
                }
                catch
                {
                    var files = await _gitHubClient.PullRequest.Files(_repoOwner, _repoName, prNumber);
                    return (files, "main"); // final fallback
                }
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
