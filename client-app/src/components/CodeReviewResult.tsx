import React from "react";
import type { CodeReview } from "../services/api";
import {
  AlertTriangle,
  CheckCircle,
  XCircle,
  Shield,
  Lightbulb,
  Code,
} from "lucide-react";
import styles from "./CodeReviewResult.module.css";

interface CodeReviewResultProps {
  review: CodeReview;
  onClose: () => void;
}

export const CodeReviewResult: React.FC<CodeReviewResultProps> = ({
  review,
  onClose,
}) => {
  // Debug logging
  console.log("🎯 CodeReviewResult component rendered");
  console.log("📋 Review prop:", review);
  console.log("📋 Review type:", typeof review);
  console.log(
    "📋 Review keys:",
    review ? Object.keys(review) : "null/undefined"
  );

  if (review) {
    console.log("🔍 Review prop breakdown:");
    console.log("  - Summary:", review.summary);
    console.log("  - Summary type:", typeof review.summary);
    console.log("  - Summary length:", review.summary?.length);
    console.log("  - Issues:", review.issues);
    console.log("  - Issues is array:", Array.isArray(review.issues));
    console.log("  - Issues length:", review.issues?.length);
    console.log("  - Suggestions:", review.suggestions);
    console.log("  - Suggestions is array:", Array.isArray(review.suggestions));
    console.log("  - Suggestions length:", review.suggestions?.length);
    console.log("  - Complexity:", review.complexity);
    console.log("  - Test Coverage:", review.testCoverage);
    console.log("  - Security:", review.security);
    console.log("  - Security is array:", Array.isArray(review.security));
    console.log("  - Security length:", review.security?.length);
  }

  const getSeverityIcon = (severity: string) => {
    switch (severity.toLowerCase()) {
      case "critical":
        return <XCircle className={styles.iconCritical} />;
      case "high":
        return <AlertTriangle className={styles.iconHigh} />;
      case "medium":
        return <AlertTriangle className={styles.iconMediumSeverity} />;
      case "low":
        return <AlertTriangle className={styles.iconLow} />;
      default:
        return <AlertTriangle className={styles.iconDefault} />;
    }
  };

  const getSeverityClass = (severity: string) => {
    switch (severity.toLowerCase()) {
      case "critical":
        return styles.issueItemCritical;
      case "high":
        return styles.issueItemHigh;
      case "medium":
        return styles.issueItemMediumSeverity;
      case "low":
        return styles.issueItemLow;
      default:
        return styles.issueItemDefault;
    }
  };

  const getComplexityColor = (complexity: string) => {
    if (!complexity) return styles.complexityDefault;
    switch (complexity.toLowerCase()) {
      case "high":
        return styles.complexityHigh;
      case "medium":
        return styles.complexityMedium;
      case "low":
        return styles.complexityLow;
      default:
        return styles.complexityDefault;
    }
  };

  return (
    <div className={styles.overlay}>
      <div className={styles.modal}>
        <div className={styles.header}>
          <div className={styles.headerContent}>
            <h2 className={styles.title}>
              <Code className={styles.iconLarge} />
              Code Review Results
            </h2>
            <button onClick={onClose} className={styles.closeButton}>
              <XCircle className={styles.iconLarge} />
            </button>
          </div>
        </div>

        <div className={styles.content}>
          {/* Debug Information */}
          <div className={styles.debugCard}>
            <h3 className={styles.debugTitle}>Debug Information</h3>
            <div className={styles.debugContent}>
              <div>Review object exists: {review ? "✅ Yes" : "❌ No"}</div>
              {review && (
                <>
                  <div>
                    Summary exists: {review.summary ? "✅ Yes" : "❌ No"}
                  </div>
                  <div>
                    Summary content:{" "}
                    {review.summary
                      ? `"${review.summary.substring(0, 100)}${
                          review.summary.length > 100 ? "..." : ""
                        }"`
                      : "Empty"}
                  </div>
                  <div>Issues count: {review.issues?.length || 0}</div>
                  <div>
                    Suggestions count: {review.suggestions?.length || 0}
                  </div>
                  <div>
                    Security issues count: {review.security?.length || 0}
                  </div>
                  <div>Complexity: {review.complexity || "None"}</div>
                  <div>Test Coverage: {review.testCoverage || "None"}</div>
                </>
              )}
            </div>
          </div>

          {/* Show message if no review data */}
          {!review && (
            <div className={styles.errorCard}>
              <h3 className={styles.errorTitle}>No Review Data</h3>
              <p className={styles.errorText}>
                The code review response is empty or invalid. This could be due
                to:
              </p>
              <ul className={styles.errorList}>
                <li>AI service returned an empty response</li>
                <li>Network error during the request</li>
                <li>Backend processing error</li>
                <li>Invalid response format</li>
              </ul>
            </div>
          )}

          {/* Show message if review exists but has no content */}
          {review &&
            !review.summary &&
            (!review.issues || review.issues.length === 0) &&
            (!review.suggestions || review.suggestions.length === 0) &&
            (!review.security || review.security.length === 0) && (
              <div className={styles.warningCard}>
                <h3 className={styles.warningTitle}>Empty Review Content</h3>
                <p className={styles.warningText}>
                  The AI returned a valid response structure but with no
                  content. This could mean:
                </p>
                <ul className={styles.warningList}>
                  <li>The code changes are too minimal to analyze</li>
                  <li>AI service had an internal processing error</li>
                  <li>The commit/PR has no reviewable code changes</li>
                  <li>Configuration issue with the AI service</li>
                </ul>
              </div>
            )}

          {/* Summary */}
          {review?.summary && (
            <div className={styles.card}>
              <h3 className={styles.sectionTitle}>Summary</h3>
              <p className={styles.summaryText}>{review.summary}</p>

              <div className={styles.summaryMetrics}>
                <div className={styles.metricGroup}>
                  <span className={styles.metricLabel}>Complexity:</span>
                  <span
                    className={`${styles.complexityBadge} ${getComplexityColor(
                      review.complexity
                    )}`}
                  >
                    {review.complexity || "Not specified"}
                  </span>
                </div>
                {review.testCoverage && (
                  <div className={styles.metricGroup}>
                    <span className={styles.metricLabel}>Test Coverage:</span>
                    <span className={styles.metricValue}>
                      {review.testCoverage}
                    </span>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Code Issues */}
          {review.issues && review.issues.length > 0 && (
            <div className={styles.card}>
              <h3 className={styles.sectionTitleWithIcon}>
                <AlertTriangle className={styles.iconMedium} />
                Code Issues ({review.issues.length})
              </h3>
              <div className={styles.issuesList}>
                {review.issues.map((issue, index) => (
                  <div key={index} className={getSeverityClass(issue.severity)}>
                    <div className={styles.issueContent}>
                      {getSeverityIcon(issue.severity)}
                      <div className={styles.issueDetails}>
                        <div className={styles.issueHeader}>
                          <span className={styles.issueLocation}>
                            {issue.file}:{issue.line}
                          </span>
                          <span
                            className={
                              issue.severity.toLowerCase() === "critical"
                                ? styles.severityBadgeCritical
                                : issue.severity.toLowerCase() === "high"
                                ? styles.severityBadgeHigh
                                : issue.severity.toLowerCase() === "medium"
                                ? styles.severityBadgeMedium
                                : styles.severityBadgeLow
                            }
                          >
                            {issue.severity}
                          </span>
                        </div>
                        <p className={styles.issueMessage}>{issue.message}</p>
                        {issue.suggestion && (
                          <div className={styles.issueSuggestion}>
                            <strong>Suggestion:</strong> {issue.suggestion}
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Security Issues */}
          {review.security && review.security.length > 0 && (
            <div className={styles.card}>
              <h3 className={styles.sectionTitleWithIcon}>
                <Shield className={styles.iconMedium} />
                Security Issues ({review.security.length})
              </h3>
              <div className={styles.issuesList}>
                {review.security.map((issue, index) => (
                  <div key={index} className={getSeverityClass(issue.severity)}>
                    <div className={styles.issueContent}>
                      {getSeverityIcon(issue.severity)}
                      <div className={styles.issueDetails}>
                        <div className={styles.issueHeader}>
                          <span className={styles.issueLocation}>
                            {issue.type}
                          </span>
                          <span
                            className={
                              issue.severity.toLowerCase() === "critical"
                                ? styles.severityBadgeCritical
                                : issue.severity.toLowerCase() === "high"
                                ? styles.severityBadgeHigh
                                : issue.severity.toLowerCase() === "medium"
                                ? styles.severityBadgeMedium
                                : styles.severityBadgeLow
                            }
                          >
                            {issue.severity}
                          </span>
                        </div>
                        <p className={styles.issueMessage}>
                          {issue.description}
                        </p>
                        <div className={styles.issueSuggestion}>
                          <strong>Recommendation:</strong>{" "}
                          {issue.recommendation}
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Suggestions */}
          {review.suggestions && review.suggestions.length > 0 && (
            <div className={styles.card}>
              <h3 className={styles.sectionTitleWithIcon}>
                <Lightbulb className={styles.iconMedium} />
                Suggestions ({review.suggestions.length})
              </h3>
              <ul className={styles.suggestionsList}>
                {review.suggestions.map((suggestion, index) => (
                  <li key={index} className={styles.suggestionItem}>
                    <CheckCircle className={styles.iconSuccess} />
                    <span className={styles.suggestionText}>{suggestion}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>

        <div className={styles.footer}>
          <div className={styles.footerContent}>
            <button onClick={onClose} className="btn-primary">
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
