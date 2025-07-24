import React from "react";
import type { Repository } from "../services/api";
import { GitBranch, ExternalLink, Lock, Unlock } from "lucide-react";

interface RepositoryCardProps {
  repository: Repository;
  onSelect: (repository: Repository) => void;
  isSelected: boolean;
}

export const RepositoryCard: React.FC<RepositoryCardProps> = ({
  repository,
  onSelect,
  isSelected,
}) => {
  const handleSelect = () => {
    onSelect(repository);
  };

  return (
    <div
      className={`card cursor-pointer transition-all duration-200 hover:shadow-xl ${
        isSelected ? "ring-2 ring-blue-500 bg-blue-50 dark:bg-blue-900" : ""
      }`}
      onClick={handleSelect}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-2">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
              {repository.name}
            </h3>
            {repository.private ? (
              <Lock className="w-4 h-4 text-gray-500" />
            ) : (
              <Unlock className="w-4 h-4 text-gray-500" />
            )}
          </div>

          <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">
            {repository.fullName}
          </p>

          {repository.description && (
            <p className="text-sm text-gray-700 dark:text-gray-300 mb-3">
              {repository.description}
            </p>
          )}

          <div className="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400">
            <div className="flex items-center gap-1">
              <GitBranch className="w-3 h-3" />
              <span>{repository.defaultBranch}</span>
            </div>
            <span
              className={`status-${repository.private ? "warning" : "success"}`}
            >
              {repository.private ? "Private" : "Public"}
            </span>
          </div>
        </div>

        <a
          href={repository.htmlUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
          onClick={(e) => e.stopPropagation()}
        >
          <ExternalLink className="w-4 h-4" />
        </a>
      </div>
    </div>
  );
};
