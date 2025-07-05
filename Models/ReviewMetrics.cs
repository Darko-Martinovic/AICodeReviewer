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
        public decimal EstimatedCost { get; set; }
        public string ReviewType { get; set; } = string.Empty;

        public double IssuesPerFile => FilesReviewed > 0 ? (double)IssuesFound / FilesReviewed : 0;
        public double LinesPerMinute => Duration.TotalMinutes > 0 ? TotalLinesOfCode / Duration.TotalMinutes : 0;
        public decimal CostPerIssue => IssuesFound > 0 ? EstimatedCost / IssuesFound : 0;

        public override string ToString()
        {
            return $@"
📊 REVIEW PERFORMANCE METRICS
┌─────────────────────────────────────────┐
│ Duration: {Duration:mm\:ss}                     │
│ Files Reviewed: {FilesReviewed}                      │
│ Issues Found: {IssuesFound}                        │
│ Lines of Code: {TotalLinesOfCode:N0}                 │
│ Tokens Used: {TokensUsed:N0}                    │
│ Estimated Cost: ${EstimatedCost:F4}             │
├─────────────────────────────────────────┤
│ EFFICIENCY METRICS                      │
│ Issues/File: {IssuesPerFile:F1}                    │
│ Lines/Minute: {LinesPerMinute:F0}                │
│ Cost/Issue: ${CostPerIssue:F4}                  │
└─────────────────────────────────────────┘";
        }
    }
}
