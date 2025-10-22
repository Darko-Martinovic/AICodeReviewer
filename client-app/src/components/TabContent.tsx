import React from "react";
import { GitCommit, GitPullRequest, Settings } from "lucide-react";
import type { Repository, Commit, PullRequest } from "../services/api";
import { RepositoryCard } from "./RepositoryCard";
import { CommitCard } from "./CommitCard";
import { PullRequestCard } from "./PullRequestCard";
import { BranchSelector } from "./BranchSelector";
import SystemPromptsManager from "./SystemPromptsManagerFixed";
import WorkflowManager from "./WorkflowManager";
import RepositoryFilterSettings from "./RepositoryFilterSettings";
import { LoadingSpinner, EmptyState } from "./UI";
import styles from "./TabContent.module.css";

type TabType =
  | "repositories"
  | "commits"
  | "pullrequests"
  | "systemprompts"
  | "workflows"
  | "repositoryfilters";

interface TabContentProps {
  activeTab: TabType;
  currentRepository: Repository | null;
  repositories: Repository[];
  commits: Commit[];
  pullRequests: PullRequest[];
  branches: string[];
  selectedBranch: string;
  loading: {
    repositories: boolean;
    commits: boolean;
    pullRequests: boolean;
    branches: boolean;
    review: boolean;
  };
  reviewingCommits: Set<string>;
  reviewingPRs: Set<number>;
  onRepositorySelect: (repository: Repository) => void;
  onCommitReview: (sha: string) => void;
  onCommitCollaborate?: (commit: Commit) => void;
  onPullRequestReview: (number: number) => void;
  onBranchSelect: (branch: string) => void;
  onAddRepository: () => void;
  onTabChange: (tab: TabType) => void;
  onRepositoryFiltersChanged?: () => void;
}

export const TabContent: React.FC<TabContentProps> = ({
  activeTab,
  currentRepository,
  repositories,
  commits,
  pullRequests,
  branches,
  selectedBranch,
  loading,
  reviewingCommits,
  reviewingPRs,
  onRepositorySelect,
  onCommitReview,
  onCommitCollaborate,
  onPullRequestReview,
  onBranchSelect,
  onAddRepository,
  onTabChange,
  onRepositoryFiltersChanged,
}) => {
  // Repositories Tab
  if (activeTab === "repositories") {
    return (
      <div>
        {loading.repositories ? (
          <LoadingSpinner />
        ) : repositories.length === 0 ? (
          <EmptyState
            icon={<Settings className="w-12 h-12 text-gray-400" />}
            title="No repositories found"
            description="No repositories match your search criteria or none are configured."
            action={{
              label: "Add Repository",
              onClick: onAddRepository,
            }}
          />
        ) : (
          <div className={styles.repositoriesGrid}>
            {repositories.map((repo) => (
              <RepositoryCard
                key={repo.id}
                repository={repo}
                onSelect={onRepositorySelect}
                isSelected={currentRepository?.id === repo.id}
              />
            ))}
          </div>
        )}
      </div>
    );
  }

  // Commits Tab
  if (activeTab === "commits") {
    return (
      <div>
        {!currentRepository ? (
          <EmptyState
            icon={<GitCommit className="w-12 h-12 text-gray-400" />}
            title="No repository selected"
            description="Please select a repository to view commits."
            action={{
              label: "Select Repository",
              onClick: () => onTabChange("repositories"),
            }}
          />
        ) : loading.commits ? (
          <LoadingSpinner />
        ) : commits.length === 0 ? (
          <EmptyState
            icon={<GitCommit className="w-12 h-12 text-gray-400" />}
            title="No commits found"
            description="No commits match your search criteria or the repository has no recent commits."
          />
        ) : (
          <div>
            {/* Branch Selector */}
            <BranchSelector
              branches={branches}
              selectedBranch={selectedBranch}
              onBranchSelect={onBranchSelect}
              loading={loading.branches}
            />

            {/* Demo Mode Info Banner */}
            <div className={styles.demoModeInfo}>
              <div className={styles.demoModeInfoContent}>
                <GitCommit className={styles.demoModeIcon} />
                <p className={styles.demoModeText}>
                  <strong>Demo Mode:</strong> Showing the last 20 commits only.
                  {commits.length === 20 &&
                    " This repository may have additional commits."}
                </p>
              </div>
            </div>

            <div className={styles.itemsList}>
              {commits.map((commit) => (
                <CommitCard
                  key={commit.sha}
                  commit={commit}
                  onReview={onCommitReview}
                  onCollaborate={onCommitCollaborate}
                  isReviewing={reviewingCommits.has(commit.sha)}
                />
              ))}
            </div>
          </div>
        )}
      </div>
    );
  }

  // Pull Requests Tab
  if (activeTab === "pullrequests") {
    return (
      <div>
        {!currentRepository ? (
          <EmptyState
            icon={<GitPullRequest className="w-12 h-12 text-gray-400" />}
            title="No repository selected"
            description="Please select a repository to view pull requests."
            action={{
              label: "Select Repository",
              onClick: () => onTabChange("repositories"),
            }}
          />
        ) : loading.pullRequests ? (
          <LoadingSpinner />
        ) : pullRequests.length === 0 ? (
          <EmptyState
            icon={<GitPullRequest className="w-12 h-12 text-gray-400" />}
            title="No pull requests found"
            description="No pull requests match your search criteria or the repository has no pull requests."
          />
        ) : (
          <div className={styles.pullRequestsList}>
            {pullRequests.map((pr) => (
              <PullRequestCard
                key={pr.number}
                pullRequest={pr}
                onReview={onPullRequestReview}
                isReviewing={reviewingPRs.has(pr.number)}
              />
            ))}
          </div>
        )}
      </div>
    );
  }

  // System Prompts Tab
  if (activeTab === "systemprompts") {
    return <SystemPromptsManager />;
  }

  // Repository Filters Tab
  if (activeTab === "repositoryfilters") {
    return (
      <RepositoryFilterSettings onFiltersChanged={onRepositoryFiltersChanged} />
    );
  }

  // Workflows Tab
  if (activeTab === "workflows") {
    return <WorkflowManager />;
  }

  // Fallback
  return null;
};

export default TabContent;
