using System.Diagnostics;

namespace AICodeReviewer.Models
{
    public class ReviewMetrics
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public int FilesReviewed { get; set; }
        public int IssuesFound { get; set; }
        public int TotalLinesOfCode { get; set; }
        public int TokensUsed { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public decimal EstimatedCost { get; set; }
        public string ReviewType { get; set; } = string.Empty;

        // Cost calculation constants (GPT-3.5-Turbo pricing)
        private const decimal INPUT_TOKEN_COST_PER_1K = 0.0015m;
        private const decimal OUTPUT_TOKEN_COST_PER_1K = 0.002m;

        public double IssuesPerFile => FilesReviewed > 0 ? (double)IssuesFound / FilesReviewed : 0;
        public double LinesPerMinute =>
            Duration.TotalMinutes > 0 ? TotalLinesOfCode / Duration.TotalMinutes : 0;
        public decimal CostPerIssue => IssuesFound > 0 ? EstimatedCost / IssuesFound : 0;

        public override string ToString()
        {
            return $@"
📊 REVIEW PERFORMANCE METRICS
Duration: {Duration:mm\:ss}
Files Reviewed: {FilesReviewed}
Issues Found: {IssuesFound}
Lines of Code: {TotalLinesOfCode:N0}
Total Tokens: {TokensUsed:N0}
Input Tokens: {InputTokens:N0}
Output Tokens: {OutputTokens:N0}
Estimated Cost: ${EstimatedCost:F4}
────────────────────────────────────────
EFFICIENCY METRICS
Issues/File: {IssuesPerFile:F1}
Lines/Minute: {LinesPerMinute:F0}
Cost/Issue: ${CostPerIssue:F4}
Cost/File: ${(FilesReviewed > 0 ? EstimatedCost / FilesReviewed : 0):F4}";
        }

        /// <summary>
        /// Initialize metrics for a new review
        /// </summary>
        public static ReviewMetrics StartReview(string reviewType)
        {
            return new ReviewMetrics { StartTime = DateTime.Now, ReviewType = reviewType };
        }

        /// <summary>
        /// Add usage data from an AI API call
        /// </summary>
        public void AddUsage(Usage usage)
        {
            InputTokens += usage.prompt_tokens;
            OutputTokens += usage.completion_tokens;
            TokensUsed += usage.total_tokens;
            EstimatedCost = CalculateCost(InputTokens, OutputTokens);
        }

        /// <summary>
        /// Calculate cost based on input and output tokens
        /// </summary>
        private static decimal CalculateCost(int inputTokens, int outputTokens)
        {
            return (inputTokens / 1000m * INPUT_TOKEN_COST_PER_1K)
                + (outputTokens / 1000m * OUTPUT_TOKEN_COST_PER_1K);
        }

        /// <summary>
        /// Finish the review and calculate final metrics
        /// </summary>
        public void FinishReview()
        {
            EndTime = DateTime.Now;
        }
    }
}
