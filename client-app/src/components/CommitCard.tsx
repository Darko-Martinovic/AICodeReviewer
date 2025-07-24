import React from "react";
import type { Commit } from "../services/api";
import { GitCommit, ExternalLink, Calendar, User } from "lucide-react";

interface CommitCardProps {
  commit: Commit;
  onReview: (sha: string) => void;
  isReviewing: boolean;
}

export const CommitCard: React.FC<CommitCardProps> = ({
  commit,
  onReview,
  isReviewing,
}) => {
  const handleReview = () => {
    onReview(commit.sha);
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

  return (
    <div className="card">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-2">
            <GitCommit className="w-4 h-4 text-gray-500" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white line-clamp-2">
              {commit.message}
            </h3>
          </div>

          <div className="flex items-center gap-4 text-sm text-gray-600 dark:text-gray-400 mb-3">
            <div className="flex items-center gap-1">
              <User className="w-3 h-3" />
              <span>{commit.author}</span>
            </div>
            <div className="flex items-center gap-1">
              <Calendar className="w-3 h-3" />
              <span>{formatDate(commit.date)}</span>
            </div>
          </div>

          <div className="flex items-center gap-2 mb-4">
            <code className="bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded text-sm font-mono">
              {commit.sha.substring(0, 8)}
            </code>
            <a
              href={commit.htmlUrl}
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
          disabled={isReviewing}
          className={`btn-primary flex items-center gap-2 ${
            isReviewing ? "opacity-50 cursor-not-allowed" : ""
          }`}
        >
          {isReviewing && <div className="spinner" />}
          {isReviewing ? "Reviewing..." : "Review Code"}
        </button>
      </div>
    </div>
  );
};
