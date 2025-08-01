import React from "react";
import {
  FolderGit2,
  MessageSquareCode,
  GitCommit,
  GitPullRequest,
  Workflow,
  ArrowLeft,
} from "lucide-react";
import type { Repository, Commit, PullRequest } from "../services/api";
import styles from "./Navigation.module.css";

type TabType =
  | "repositories"
  | "commits"
  | "pullrequests"
  | "systemprompts"
  | "workflows";

interface NavigationProps {
  activeTab: TabType;
  onTabChange: (tab: TabType) => void;
  currentRepository: Repository | null;
  commits: Commit[];
  pullRequests: PullRequest[];
  isInRepositoryView: boolean;
  onExitRepository?: () => void;
}

export const Navigation: React.FC<NavigationProps> = ({
  activeTab,
  onTabChange,
  currentRepository,
  commits,
  pullRequests,
  isInRepositoryView,
  onExitRepository,
}) => {
  // Define main tabs (always visible)
  const mainTabs = [
    {
      id: "repositories" as TabType,
      label: "Repositories",
      icon: FolderGit2,
      count: null,
      color: "text-blue-600",
    },
    {
      id: "systemprompts" as TabType,
      label: "System Prompts",
      icon: MessageSquareCode,
      count: null,
      color: "text-purple-600",
    },
    {
      id: "workflows" as TabType,
      label: "Workflows",
      icon: Workflow,
      count: null,
      color: "text-green-600",
    },
  ];

  // Define repository-specific tabs (only visible when in repository view)
  const repositoryTabs = [
    {
      id: "commits" as TabType,
      label: "Commits",
      icon: GitCommit,
      count: commits.length,
      color: "text-orange-600",
    },
    {
      id: "pullrequests" as TabType,
      label: "Pull Requests",
      icon: GitPullRequest,
      count: pullRequests.length,
      color: "text-indigo-600",
    },
  ];

  const tabs = isInRepositoryView ? repositoryTabs : mainTabs;

  return (
    <nav className={styles.navigation}>
      <div className={styles.navigationContainer}>
        {/* Repository context header when in repository view */}
        {isInRepositoryView && currentRepository && (
          <div className={styles.repositoryHeader}>
            <button onClick={onExitRepository} className={styles.backButton}>
              <ArrowLeft className={styles.backIcon} />
              <span>Back to Repositories</span>
            </button>
            <div className={styles.repositoryInfo}>
              <h2 className={styles.repositoryName}>
                {currentRepository.fullName}
              </h2>
              <span className={styles.repositoryBranch}>
                {currentRepository.defaultBranch}
              </span>
            </div>
          </div>
        )}

        <div className={styles.tabsContainer}>
          {tabs.map((tab) => {
            const Icon = tab.icon;
            const isActive = activeTab === tab.id;

            return (
              <button
                key={tab.id}
                onClick={() => onTabChange(tab.id)}
                className={`${styles.tabButton} ${
                  isActive ? styles.tabButtonActive : styles.tabButtonInactive
                }`}
              >
                <Icon
                  className={`${styles.tabIcon} ${
                    isActive
                      ? styles.tabIconActive
                      : tab.color || "text-gray-500"
                  }`}
                />
                <span className={styles.tabLabel}>
                  {tab.label}
                  {tab.count !== null && (
                    <span className={styles.tabCount}> ({tab.count})</span>
                  )}
                </span>
              </button>
            );
          })}
        </div>
      </div>
    </nav>
  );
};

export default Navigation;
