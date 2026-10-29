using AICodeReviewer.Models.Review;

namespace AICodeReviewer.Lab
{
    /// <summary>
    /// What the judge said about one finding
    /// </summary>
    public record JudgeVerdict(int Finding, string Verdict, string LabelId, string Reason);

    /// <summary>
    /// A finding with its final verdict: match, duplicate, real (a real issue with no label) or false_alarm
    /// </summary>
    public record ScoredFinding(
        int Line, int EndLine, string Category, string Severity, string Title, string Explanation,
        string Verdict, string? LabelId, string Reason);

    public record CaseScore(IReadOnlyList<ScoredFinding> Findings, IReadOnlyList<string> Found, IReadOnlyList<string> Missed, int OutsideWindow);

    public static class Scoring
    {
        public const string Match = "match";
        public const string Duplicate = "duplicate";
        public const string Real = "real";
        public const string FalseAlarm = "false_alarm";

        /// <summary>
        /// Turns the judge's verdicts into the case's score. The judge decides whether a finding describes a labelled
        /// defect; a match only counts when the finding's lines are within tolerance of the label's, a second match of
        /// the same label is a duplicate, and a match the rules refuse becomes "real"
        /// </summary>
        public static CaseScore Score(IReadOnlyList<Label> labels, IReadOnlyList<Finding> findings, IReadOnlyList<JudgeVerdict> verdicts, int tolerance)
        {
            var byFinding = verdicts.ToDictionary(v => v.Finding);
            var found = new List<string>();
            var scored = new List<ScoredFinding>();
            var outsideWindow = 0;

            for (var i = 0; i < findings.Count; i++)
            {
                var finding = findings[i];
                var verdict = byFinding.TryGetValue(i, out var v) ? v : new JudgeVerdict(i, FalseAlarm, "", "The judge gave no verdict");
                var final = verdict.Verdict;
                string? labelId = null;
                var reason = verdict.Reason;

                if (final == Match)
                {
                    var label = labels.FirstOrDefault(l => l.Id == verdict.LabelId);
                    if (label is null)
                    {
                        final = Real;
                        reason = $"The judge named an unknown label '{verdict.LabelId}'. {reason}";
                    }
                    else if (!Overlaps(finding, label, tolerance))
                    {
                        final = Real;
                        outsideWindow++;
                        reason = $"Describes {label.Id} but at lines {finding.Line}-{finding.EndLine}, outside the window. {reason}";
                    }
                    else
                    {
                        labelId = label.Id;
                        if (found.Contains(label.Id))
                        {
                            final = Duplicate;
                        }
                        else
                        {
                            found.Add(label.Id);
                        }
                    }
                }
                else if (final is not (Real or FalseAlarm))
                {
                    reason = $"Unknown verdict '{final}'. {reason}";
                    final = FalseAlarm;
                }

                scored.Add(new ScoredFinding(finding.Line, finding.EndLine, finding.Category, finding.Severity, finding.Title,
                    finding.Explanation, final, labelId, reason));
            }

            var missed = labels.Select(l => l.Id).Where(id => !found.Contains(id)).ToList();
            return new CaseScore(scored, found, missed, outsideWindow);
        }

        public static bool Overlaps(Finding finding, Label label, int tolerance)
        {
            var end = Math.Max(finding.Line, finding.EndLine);
            return finding.Line <= label.EndLine + tolerance && end >= label.Line - tolerance;
        }
    }

    /// <summary>
    /// Totals and rates over a set of cases. Precision counts every finding that is not a false alarm, since a real
    /// issue without a label is still a good finding; strict precision counts only matches of labels
    /// </summary>
    public record Metrics(
        int Cases, int Errors, int Labels, int Found, int Findings, int Matched, int Duplicates, int Real, int FalseAlarms,
        int OutsideWindow, int CleanCases, int FalseAlarmsOnClean,
        double Recall, double Precision, double StrictPrecision, double F1, double FalseAlarmsPerCleanCase,
        decimal ReviewerCost, decimal JudgeCost)
    {
        public static Metrics Of(IReadOnlyCollection<CaseResult> cases)
        {
            var findings = cases.SelectMany(c => c.Findings).ToList();
            int Count(string verdict) => findings.Count(f => f.Verdict == verdict);

            var labels = cases.Sum(c => c.Labels.Count);
            var found = cases.Sum(c => c.Found.Count);
            var falseAlarms = Count(Scoring.FalseAlarm);
            var clean = cases.Where(c => c.Kind == "clean").ToList();
            var falseAlarmsOnClean = clean.SelectMany(c => c.Findings).Count(f => f.Verdict == Scoring.FalseAlarm);

            var recall = labels == 0 ? 1 : (double)found / labels;
            var precision = findings.Count == 0 ? 1 : (double)(findings.Count - falseAlarms) / findings.Count;
            var strict = findings.Count == 0 ? 1 : (double)(Count(Scoring.Match) + Count(Scoring.Duplicate)) / findings.Count;
            var f1 = recall + precision == 0 ? 0 : 2 * recall * precision / (recall + precision);

            return new Metrics(
                cases.Count, cases.Count(c => c.Error is not null), labels, found, findings.Count,
                Count(Scoring.Match), Count(Scoring.Duplicate), Count(Scoring.Real), falseAlarms,
                cases.Sum(c => c.OutsideWindow), clean.Count, falseAlarmsOnClean,
                Math.Round(recall, 4), Math.Round(precision, 4), Math.Round(strict, 4), Math.Round(f1, 4),
                clean.Count == 0 ? 0 : Math.Round((double)falseAlarmsOnClean / clean.Count, 4),
                cases.Sum(c => c.ReviewerCost), cases.Sum(c => c.JudgeCost));
        }
    }

    /// <summary>
    /// One case of a run, as saved in the results file
    /// </summary>
    public record CaseResult(
        string Id, string Group, string Kind, int Repeat, string PromptHash,
        IReadOnlyList<string> Labels, IReadOnlyList<ScoredFinding> Findings, IReadOnlyList<string> Found, IReadOnlyList<string> Missed,
        int OutsideWindow, int ReviewerInputTokens, int ReviewerOutputTokens, decimal ReviewerCost,
        int JudgeInputTokens, int JudgeOutputTokens, decimal JudgeCost, double Seconds, string? Error);
}
