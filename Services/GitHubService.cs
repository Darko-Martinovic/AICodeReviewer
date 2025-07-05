using Octokit;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for handling GitHub API interactions
    /// </summary>
    public class GitHubService
    {
        private readonly GitHubClient _gitHubClient;
        private readonly string _repoOwner;
        private readonly string _repoName;

        public GitHubService(string token, string repoOwner, string repoName)
        {
            _repoOwner = repoOwner ?? throw new ArgumentNullException(nameof(repoOwner));
            _repoName = repoName ?? throw new ArgumentNullException(nameof(repoName));

            _gitHubClient = new GitHubClient(new ProductHeaderValue("AICodeReviewer"));
            _gitHubClient.Credentials = new Credentials(token);
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

                Console.WriteLine($"🔍 Attempting to access repository: {_repoOwner}/{_repoName}");

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
        /// Gets all commits for the main branch
        /// </summary>
        public async Task<IReadOnlyList<GitHubCommit>> GetCommitsAsync()
        {
            return await _gitHubClient.Repository.Commit.GetAll(
                _repoOwner,
                _repoName,
                new CommitRequest { Sha = "main" }
            );
        }

        /// <summary>
        /// Gets detailed commit information including file changes
        /// </summary>
        public async Task<GitHubCommit> GetCommitDetailAsync(string sha)
        {
            return await _gitHubClient.Repository.Commit.Get(_repoOwner, _repoName, sha);
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
    }
}
