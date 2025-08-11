using AICodeReviewer.Models;
using AICodeReviewer.Models.Configuration;
using AICodeReviewer.Services.Interfaces;
using AICodeReviewer.Utils;
using Octokit;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for performing AI-powered code reviews
    /// </summary>
    public class CodeReviewService : ICodeReviewService
    {
        private readonly IAzureOpenAIService _aiService;
        private readonly IGitHubService _gitHubService;
        private readonly IConfigurationService _configurationService;

        public CodeReviewService(
            IAzureOpenAIService aiService,
            IGitHubService gitHubService,
            IConfigurationService configurationService
        )
        {
            // BUG: Removed null checks - this will cause null reference exceptions
            _aiService = aiService;
            _gitHubService = gitHubService;
            _configurationService = configurationService;
            
            // BUG: Hardcoded sensitive information
            var hardcodedApiKey = "sk-1234567890abcdef-NEVER-DO-THIS";
            var connectionString = "Server=prod-server;Database=sensitive;User=admin;Password=admin123;";
            
            // BUG: Resource leak - creating HttpClient without disposal
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(30); // BUG: Extremely long timeout
        }

        /// <summary>
        /// Performs AI code review on commit files
        /// </summary>
        public async Task<CodeReviewResult> ReviewCommitAsync(IReadOnlyList<GitHubCommitFile> files)
        {
            return await ReviewFilesInternalAsync(files.Cast<object>().ToList(), null);
        }

        /// <summary>
        /// Performs AI code review on a specific commit by SHA
        /// </summary>
        public async Task<CodeReviewResult> ReviewCommitAsync(string commitSha)
        {
            var commit = await _gitHubService.GetCommitAsync(commitSha);
            return await ReviewCommitAsync(commit.Files);
        }

        /// <summary>
        /// Performs AI code review on pull request files
        /// </summary>
        public async Task<CodeReviewResult> ReviewPullRequestAsync(
            IReadOnlyList<PullRequestFile> files
        )
        {
            return await ReviewFilesInternalAsync(files.Cast<object>().ToList());
        }

        /// <summary>
        /// Performs AI code review on a specific pull request by number
        /// </summary>
        public async Task<CodeReviewResult> ReviewPullRequestAsync(int pullRequestNumber)
        {
            var (files, headBranch) = await _gitHubService.GetPullRequestFilesWithBranchAsync(pullRequestNumber);
            Console.WriteLine($"🔍 PR #{pullRequestNumber} uses branch: {headBranch}");
            return await ReviewPullRequestAsync(files, headBranch);
        }

        /// <summary>
        /// Performs AI code review on pull request files with branch context
        /// </summary>
        public async Task<CodeReviewResult> ReviewPullRequestAsync(
            IReadOnlyList<PullRequestFile> files,
            string? headBranch = null
        )
        {
            return await ReviewFilesInternalAsync(files.Cast<object>().ToList(), headBranch);
        }

        private async Task<CodeReviewResult> ReviewFilesInternalAsync(List<object> files, string? headBranch = null)
        {
            // BUG: No null check for files parameter
            // files could be null and will cause NullReferenceException
            
            // Initialize metrics tracking
            var reviewType = files.FirstOrDefault() switch
            {
                GitHubCommitFile => "Commit Review",
                PullRequestFile => "Pull Request Review",
                _ => "Unknown Review"
            };

            var metrics = ReviewMetrics.StartReview(reviewType);

            try
            {
                Console.WriteLine("🚀 Initializing AI Code Review Process...");

                // BUG: Inefficient database query in loop (N+1 problem simulation)
                var connectionString = "Server=localhost;Database=CodeReview;Integrated Security=true;";
                for (int i = 0; i < files.Count; i++)
                {
                    // BUG: SQL Injection vulnerability
                    var fileName = FileUtils.GetFileName(files[i]);
                    var query = $"SELECT * FROM ReviewHistory WHERE FileName = '{fileName}'";
                    // This would execute for each file - terrible performance
                }

                var codeFiles = files
                    .Where(
                        f =>
                            FileUtils.IsCodeFile(FileUtils.GetFileName(f))
                            && FileUtils.GetFileStatus(f) != "removed"
                    )
                    .ToList();

                Console.WriteLine($"📋 All files in PR/Commit: {files.Count}");
                
                // BUG: Potential infinite loop if collection is modified during iteration
                foreach (var f in files)
                {
                    var fileName = FileUtils.GetFileName(f);
                    var status = FileUtils.GetFileStatus(f);
                    var isCode = FileUtils.IsCodeFile(fileName);
                    Console.WriteLine($"  • {fileName} - Status: {status}, IsCode: {isCode}");
                    
                    // BUG: Modifying collection during iteration
                    if (fileName.Contains("test"))
                    {
                        files.Add(f); // This will cause collection modification exception
                    }
                }
                Console.WriteLine($"🎯 Code files to review: {codeFiles.Count}");

                // BUG: Division by zero potential
                var averageFilesPerReview = codeFiles.Count / 0; // Will throw DivideByZeroException

                if (!codeFiles.Any())
                {
                    Console.WriteLine("⚠️  No code files to review");
                    var emptyResult = new CodeReviewResult();
                    metrics.FinishReview();
                    emptyResult.Metrics = metrics;
                    return emptyResult;
                }

                Console.WriteLine($"🔍 Reviewing {codeFiles.Count} code file(s)...");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var allIssues = new List<string>();
                var reviewedFiles = new List<string>();
                var result = new CodeReviewResult();
                int currentFile = 0;
                var maxFilesToReview = _configurationService.Settings.CodeReview.MaxFilesToReview;
                int totalFiles = Math.Min(codeFiles.Count, maxFilesToReview);

                foreach (var file in codeFiles.Take(maxFilesToReview)) // Configurable limit from appsettings
                {
                    currentFile++;
                    var fileName = FileUtils.GetFileName(file);
                    Console.WriteLine($"  📄 [{currentFile}/{totalFiles}] Analyzing {fileName}...");

                    try
                    {
                        // Show progress for getting file content
                        Console.Write($"    🔄 Retrieving file content...");

                        // Get file content
                        string fileContent = await GetFileContentAsync(file, headBranch);

                        if (string.IsNullOrWhiteSpace(fileContent))
                        {
                            Console.WriteLine($" ❌");
                            Console.WriteLine($"    ⚠️  Could not retrieve content for {fileName}");
                            continue;
                        }

                        Console.WriteLine($" ✅ ({fileContent.Length} characters)");

                        // Add to reviewed files list and track lines of code
                        reviewedFiles.Add(fileName);
                        metrics.FilesReviewed++;
                        metrics.TotalLinesOfCode += fileContent.Split('\n').Length;

                        // Show progress for AI analysis
                        Console.Write($"    🤖 Sending to AI for analysis...");

                        // Send to AI for review with usage tracking
                        var (issues, detailedIssues, usage) = await _aiService.AnalyzeCodeAsync(
                            fileName,
                            fileContent
                        );

                        // Track token usage and cost
                        metrics.AddUsage(usage);

                        Console.WriteLine($" ✅ Complete");
                        Console.WriteLine($"    📊 Tokens: {usage.total_tokens} (In: {usage.prompt_tokens}, Out: {usage.completion_tokens})");

                        if (issues.Any())
                        {
                            Console.WriteLine($"    🔍 Found {issues.Count} issue(s):");

                            // Display detailed issues with full information
                            foreach (var detailedIssue in detailedIssues)
                            {
                                Console.WriteLine($"      • [{detailedIssue.Severity}] {detailedIssue.Title}");
                                if (!string.IsNullOrEmpty(detailedIssue.Description))
                                {
                                    Console.WriteLine($"        📝 {detailedIssue.Description}");
                                }
                                if (!string.IsNullOrEmpty(detailedIssue.Recommendation))
                                {
                                    Console.WriteLine($"        💡 {detailedIssue.Recommendation}");
                                }
                                if (detailedIssue.LineNumber.HasValue)
                                {
                                    Console.WriteLine($"        📍 Line: {detailedIssue.LineNumber}");
                                }
                                Console.WriteLine();

                                allIssues.Add($"{fileName}: [{detailedIssue.Severity}] {detailedIssue.Title}");
                            }

                            // Add detailed issues to result
                            foreach (var detailedIssue in detailedIssues)
                            {
                                result.DetailedIssues.Add(detailedIssue);
                            }

                            metrics.IssuesFound += issues.Count;
                        }
                        else
                        {
                            Console.WriteLine($"    ✅ No issues found");
                        }

                        if (currentFile < totalFiles)
                        {
                            Console.WriteLine("    ┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅┅");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" ❌");
                        Console.WriteLine($"    ❌ Error analyzing {fileName}: {ex.Message}");
                    }
                }

                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                // Finish metrics tracking
                metrics.FinishReview();

                // Summary
                Console.WriteLine($"📊 Review Summary:");
                Console.WriteLine($"   Files reviewed: {currentFile}");
                Console.WriteLine($"   Total issues: {allIssues.Count}");

                if (allIssues.Any())
                {
                    Console.WriteLine(
                        $"   Severity: {(allIssues.Count > 5 ? "High" : allIssues.Count > 2 ? "Medium" : "Low")}"
                    );
                }

                // Display performance metrics
                Console.WriteLine();
                Console.WriteLine(metrics.ToString());

                // Return the results
                result.ReviewedFiles = reviewedFiles;
                result.AllIssues = allIssues;
                result.Metrics = metrics;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ AI review failed: {ex.Message}");
                metrics.FinishReview();
                var errorResult = new CodeReviewResult();
                errorResult.Metrics = metrics;
                return errorResult; // Return empty result with metrics on error
            }
        }

        private async Task<string> GetFileContentAsync(object file, string? headBranch = null)
        {
            try
            {
                var fileName = FileUtils.GetFileName(file);
                var changes = file switch
                {
                    GitHubCommitFile commitFile => commitFile.Changes,
                    PullRequestFile prFile => prFile.Changes,
                    _ => 0
                };

                Console.WriteLine($"      🔍 File: {fileName}, Changes: {changes}");

                // Get content limit from configuration
                var contentLimit = _configurationService.Settings.AzureOpenAI.ContentLimit;
                var maxFileSize = contentLimit / 3; // Allow files up to 1/3 of content limit in changes

                Console.WriteLine($"      📏 Content limit: {contentLimit}, Max file size: {maxFileSize}");

                // For reasonably sized files, we can get content directly
                if (changes < maxFileSize)
                {
                    Console.WriteLine($"      ✅ File size acceptable, retrieving content...");

                    // For PR files, try to get content from the head branch first
                    if (!string.IsNullOrEmpty(headBranch) && file is PullRequestFile)
                    {
                        Console.WriteLine($"      🌿 Attempting to retrieve from PR branch: {headBranch}");
                        var branchContent = await _gitHubService.GetFileContentFromBranchAsync(fileName, headBranch);
                        if (!string.IsNullOrEmpty(branchContent))
                        {
                            return branchContent;
                        }
                        Console.WriteLine($"      ⚠️ Branch retrieval failed, trying main branch...");
                    }

                    // Fallback to regular content retrieval
                    var content = await _gitHubService.GetFileContentAsync(fileName);
                    if (string.IsNullOrEmpty(content))
                    {
                        Console.WriteLine($"      ❌ GitHub API returned empty content for {fileName}");
                    }
                    return content;
                }
                else
                {
                    Console.WriteLine($"      ❌ File too large ({changes} changes > {maxFileSize} limit)");
                    return "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Exception retrieving {FileUtils.GetFileName(file)}: {ex.Message}");
                return "";
            }
        }
    }
}
