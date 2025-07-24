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
        return <XCircle className="w-4 h-4 text-red-600" />;
      case "high":
        return <AlertTriangle className="w-4 h-4 text-red-500" />;
      case "medium":
        return <AlertTriangle className="w-4 h-4 text-yellow-500" />;
      case "low":
        return <AlertTriangle className="w-4 h-4 text-blue-500" />;
      default:
        return <AlertTriangle className="w-4 h-4 text-gray-500" />;
    }
  };

  const getSeverityClass = (severity: string) => {
    switch (severity.toLowerCase()) {
      case "critical":
        return "border-red-500 bg-red-50 dark:bg-red-900";
      case "high":
        return "border-red-400 bg-red-50 dark:bg-red-900";
      case "medium":
        return "border-yellow-400 bg-yellow-50 dark:bg-yellow-900";
      case "low":
        return "border-blue-400 bg-blue-50 dark:bg-blue-900";
      default:
        return "border-gray-400 bg-gray-50 dark:bg-gray-900";
    }
  };

  const getComplexityColor = (complexity: string) => {
    if (!complexity) return "text-gray-600 bg-gray-100";
    switch (complexity.toLowerCase()) {
      case "high":
        return "text-red-600 bg-red-100";
      case "medium":
        return "text-yellow-600 bg-yellow-100";
      case "low":
        return "text-green-600 bg-green-100";
      default:
        return "text-gray-600 bg-gray-100";
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-y-auto">
        <div className="p-6 border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between">
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
              <Code className="w-6 h-6" />
              Code Review Results
            </h2>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
            >
              <XCircle className="w-6 h-6" />
            </button>
          </div>
        </div>

        <div className="p-6 space-y-6">
          {/* Debug Information */}
          <div className="card bg-yellow-50 border-yellow-200 dark:bg-yellow-900/20 dark:border-yellow-800">
            <h3 className="text-lg font-semibold text-yellow-800 dark:text-yellow-200 mb-3">
              Debug Information
            </h3>
            <div className="text-sm space-y-1 font-mono">
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
            <div className="card border-red-200 bg-red-50 dark:bg-red-900/20 dark:border-red-800">
              <h3 className="text-lg font-semibold text-red-800 dark:text-red-200 mb-3">
                No Review Data
              </h3>
              <p className="text-red-700 dark:text-red-300">
                The code review response is empty or invalid. This could be due
                to:
              </p>
              <ul className="list-disc list-inside mt-2 text-red-700 dark:text-red-300 text-sm">
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
              <div className="card border-yellow-200 bg-yellow-50 dark:bg-yellow-900/20 dark:border-yellow-800">
                <h3 className="text-lg font-semibold text-yellow-800 dark:text-yellow-200 mb-3">
                  Empty Review Content
                </h3>
                <p className="text-yellow-700 dark:text-yellow-300">
                  The AI returned a valid response structure but with no
                  content. This could mean:
                </p>
                <ul className="list-disc list-inside mt-2 text-yellow-700 dark:text-yellow-300 text-sm">
                  <li>The code changes are too minimal to analyze</li>
                  <li>AI service had an internal processing error</li>
                  <li>The commit/PR has no reviewable code changes</li>
                  <li>Configuration issue with the AI service</li>
                </ul>
              </div>
            )}

          {/* Summary */}
          {review?.summary && (
            <div className="card">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
                Summary
              </h3>
              <p className="text-gray-700 dark:text-gray-300">
                {review.summary}
              </p>

              <div className="flex items-center gap-4 mt-4">
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                    Complexity:
                  </span>
                  <span
                    className={`px-2 py-1 rounded-full text-sm font-medium ${getComplexityColor(
                      review.complexity
                    )}`}
                  >
                    {review.complexity || "Not specified"}
                  </span>
                </div>
                {review.testCoverage && (
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                      Test Coverage:
                    </span>
                    <span className="text-sm font-medium text-gray-900 dark:text-white">
                      {review.testCoverage}
                    </span>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Code Issues */}
          {review.issues && review.issues.length > 0 && (
            <div className="card">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <AlertTriangle className="w-5 h-5" />
                Code Issues ({review.issues.length})
              </h3>
              <div className="space-y-3">
                {review.issues.map((issue, index) => (
                  <div
                    key={index}
                    className={`border-l-4 p-4 rounded-r ${getSeverityClass(
                      issue.severity
                    )}`}
                  >
                    <div className="flex items-start gap-3">
                      {getSeverityIcon(issue.severity)}
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                            {issue.file}:{issue.line}
                          </span>
                          <span
                            className={`px-2 py-1 rounded text-xs font-medium ${
                              issue.severity.toLowerCase() === "critical"
                                ? "bg-red-100 text-red-800"
                                : issue.severity.toLowerCase() === "high"
                                ? "bg-red-100 text-red-700"
                                : issue.severity.toLowerCase() === "medium"
                                ? "bg-yellow-100 text-yellow-700"
                                : "bg-blue-100 text-blue-700"
                            }`}
                          >
                            {issue.severity}
                          </span>
                        </div>
                        <p className="text-gray-800 dark:text-gray-200 mb-2">
                          {issue.message}
                        </p>
                        {issue.suggestion && (
                          <div className="bg-gray-100 dark:bg-gray-700 p-2 rounded text-sm">
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
            <div className="card">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <Shield className="w-5 h-5" />
                Security Issues ({review.security.length})
              </h3>
              <div className="space-y-3">
                {review.security.map((issue, index) => (
                  <div
                    key={index}
                    className={`border-l-4 p-4 rounded-r ${getSeverityClass(
                      issue.severity
                    )}`}
                  >
                    <div className="flex items-start gap-3">
                      {getSeverityIcon(issue.severity)}
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                            {issue.type}
                          </span>
                          <span
                            className={`px-2 py-1 rounded text-xs font-medium ${
                              issue.severity.toLowerCase() === "critical"
                                ? "bg-red-100 text-red-800"
                                : issue.severity.toLowerCase() === "high"
                                ? "bg-red-100 text-red-700"
                                : issue.severity.toLowerCase() === "medium"
                                ? "bg-yellow-100 text-yellow-700"
                                : "bg-blue-100 text-blue-700"
                            }`}
                          >
                            {issue.severity}
                          </span>
                        </div>
                        <p className="text-gray-800 dark:text-gray-200 mb-2">
                          {issue.description}
                        </p>
                        <div className="bg-gray-100 dark:bg-gray-700 p-2 rounded text-sm">
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
            <div className="card">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <Lightbulb className="w-5 h-5" />
                Suggestions ({review.suggestions.length})
              </h3>
              <ul className="space-y-2">
                {review.suggestions.map((suggestion, index) => (
                  <li key={index} className="flex items-start gap-2">
                    <CheckCircle className="w-4 h-4 text-green-500 mt-0.5 flex-shrink-0" />
                    <span className="text-gray-700 dark:text-gray-300">
                      {suggestion}
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>

        <div className="p-6 border-t border-gray-200 dark:border-gray-700">
          <div className="flex justify-end">
            <button onClick={onClose} className="btn-primary">
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
