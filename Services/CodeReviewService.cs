using AICodeReviewer.Models;
using AICodeReviewer.Utils;
using Octokit;

namespace AICodeReviewer.Services
{
    /// <summary>
    /// Service for performing AI-powered code reviews
    /// </summary>
    public class CodeReviewService
    {
        private readonly AzureOpenAIService _aiService;
        private readonly GitHubService _gitHubService;

        public CodeReviewService(AzureOpenAIService aiService, GitHubService gitHubService)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
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
        public async Task<CodeReviewResult> ReviewPullRequestAsync(IReadOnlyList<PullRequestFile> files)
        {
            return await ReviewFilesInternalAsync(files.Cast<object>().ToList());
        }

        private async Task<CodeReviewResult> ReviewFilesInternalAsync(List<object> files)
        {
            try
            {
                Console.WriteLine("🚀 Initializing AI Code Review Process...");

                var codeFiles = files.Where(f => FileUtils.IsCodeFile(FileUtils.GetFileName(f)) && FileUtils.GetFileStatus(f) != "removed").ToList();

                if (!codeFiles.Any())
                {
                    Console.WriteLine("⚠️  No code files to review");
                    return new CodeReviewResult();
                }

                Console.WriteLine($"🔍 Reviewing {codeFiles.Count} code file(s)...");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var allIssues = new List<string>();
                var reviewedFiles = new List<string>();
                var result = new CodeReviewResult();
                int currentFile = 0;
                int totalFiles = Math.Min(codeFiles.Count, 3);

                foreach (var file in codeFiles.Take(3)) // Limit to 3 files to avoid token limits
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

                        // Add to reviewed files list
                        reviewedFiles.Add(fileName);

                        // Show progress for AI analysis
                        Console.Write($"    🤖 Sending to AI for analysis...");

                        // Send to AI for review
                        var (issues, detailedIssues) = await _aiService.AnalyzeCodeAsync(fileName, fileContent);

                        Console.WriteLine($" ✅ Complete");

                        if (issues.Any())
                        {
                            Console.WriteLine($"    🔍 Found {issues.Count} issue(s):");
                            foreach (var issue in issues)
                            {
                                Console.WriteLine($"      • {issue}");
                                allIssues.Add($"{fileName}: {issue}");
                            }

                            // Add detailed issues to result
                            foreach (var detailedIssue in detailedIssues)
                            {
                                result.DetailedIssues.Add(detailedIssue);
                            }
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
                // Summary
                Console.WriteLine($"📊 Review Summary:");
                Console.WriteLine($"   Files reviewed: {currentFile}");
                Console.WriteLine($"   Total issues: {allIssues.Count}");

                if (allIssues.Any())
                {
                    Console.WriteLine($"   Severity: {(allIssues.Count > 5 ? "High" : allIssues.Count > 2 ? "Medium" : "Low")}");
                }

                // Return the results
                result.ReviewedFiles = reviewedFiles;
                result.AllIssues = allIssues;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ AI review failed: {ex.Message}");
                return new CodeReviewResult(); // Return empty result on error
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

                // For small files, we can get content directly
                if (changes < 1000) // Avoid very large files
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
