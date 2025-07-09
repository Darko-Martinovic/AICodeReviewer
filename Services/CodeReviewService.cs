using AICodeReviewer.Models;
using AICodeReviewer.Models.Configuration;
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
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _gitHubService =
                gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
            _configurationService =
                configurationService
                ?? throw new ArgumentNullException(nameof(configurationService));
        }

        /// <summary>
        /// Performs AI code review on commit files
        /// </summary>
        public async Task<CodeReviewResult> ReviewCommitAsync(IReadOnlyList<GitHubCommitFile> files)
        {
            return await ReviewFilesInternalAsync(files.Cast<object>().ToList());
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

        private async Task<CodeReviewResult> ReviewFilesInternalAsync(List<object> files)
        {
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

                var codeFiles = files
                    .Where(
                        f =>
                            FileUtils.IsCodeFile(FileUtils.GetFileName(f))
                            && FileUtils.GetFileStatus(f) != "removed"
                    )
                    .ToList();

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
                        string fileContent = await GetFileContentAsync(file);

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

        private async Task<string> GetFileContentAsync(object file)
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

                // Get content limit from configuration
                var contentLimit = _configurationService.Settings.AzureOpenAI.ContentLimit;
                var maxFileSize = contentLimit / 3; // Allow files up to 1/3 of content limit in changes

                // For reasonably sized files, we can get content directly
                if (changes < maxFileSize)
                {
                    return await _gitHubService.GetFileContentAsync(fileName);
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}
