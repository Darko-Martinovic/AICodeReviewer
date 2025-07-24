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
          {/* Summary */}
          <div className="card">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
              Summary
            </h3>
            <p className="text-gray-700 dark:text-gray-300">{review.summary}</p>

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
                  {review.complexity}
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
