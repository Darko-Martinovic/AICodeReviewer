import React from "react";
import type { PullRequest } from "../services/api";
import {
  GitPullRequest,
  ExternalLink,
  Calendar,
  User,
  GitBranch,
} from "lucide-react";

interface PullRequestCardProps {
  pullRequest: PullRequest;
  onReview: (number: number) => void;
  isReviewing: boolean;
}

export const PullRequestCard: React.FC<PullRequestCardProps> = ({
  pullRequest,
  onReview,
  isReviewing,
}) => {
  const handleReview = () => {
    onReview(pullRequest.number);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const getStateColor = (state: string) => {
    switch (state.toLowerCase()) {
      case "open":
        return "status-success";
      case "closed":
        return "status-error";
      case "merged":
        return "status-info";
      default:
        return "status-warning";
    }
  };

  return (
    <div className="card">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-2">
            <GitPullRequest className="w-4 h-4 text-gray-500" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white line-clamp-2">
              {pullRequest.title}
            </h3>
            <span className={getStateColor(pullRequest.state)}>
              {pullRequest.state}
            </span>
          </div>

          <div className="flex items-center gap-4 text-sm text-gray-600 dark:text-gray-400 mb-3">
            <div className="flex items-center gap-1">
              <span>#{pullRequest.number}</span>
            </div>
            <div className="flex items-center gap-1">
              <User className="w-3 h-3" />
              <span>{pullRequest.author}</span>
            </div>
            <div className="flex items-center gap-1">
              <Calendar className="w-3 h-3" />
              <span>{formatDate(pullRequest.createdAt)}</span>
            </div>
          </div>

          <div className="flex items-center gap-2 mb-3">
            <GitBranch className="w-3 h-3 text-gray-500" />
            <span className="text-sm">
              <code className="bg-gray-100 dark:bg-gray-800 px-1 rounded">
                {pullRequest.headBranch}
              </code>
              <span className="mx-2">→</span>
              <code className="bg-gray-100 dark:bg-gray-800 px-1 rounded">
                {pullRequest.baseBranch}
              </code>
            </span>
          </div>

          {pullRequest.body && (
            <p className="text-sm text-gray-700 dark:text-gray-300 mb-4 line-clamp-3">
              {pullRequest.body}
            </p>
          )}

          <div className="flex items-center gap-2 mb-4">
            <a
              href={pullRequest.htmlUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
            >
              <ExternalLink className="w-3 h-3" />
            </a>
          </div>
        </div>
      </div>

      <div className="flex justify-end">
        <button
          onClick={handleReview}
          disabled={isReviewing || pullRequest.state.toLowerCase() !== "open"}
          className={`btn-primary flex items-center gap-2 ${
            isReviewing || pullRequest.state.toLowerCase() !== "open"
              ? "opacity-50 cursor-not-allowed"
              : ""
          }`}
        >
          {isReviewing && <div className="spinner" />}
          {isReviewing ? "Reviewing..." : "Review PR"}
        </button>
      </div>
    </div>
  );
};
