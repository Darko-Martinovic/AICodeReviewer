import React from "react";
import { Settings, GitCommit, GitPullRequest, Workflow } from "lucide-react";
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
}

export const Navigation: React.FC<NavigationProps> = ({
  activeTab,
  onTabChange,
  currentRepository,
  commits,
  pullRequests,
}) => {
  const tabs = [
    {
      id: "repositories" as TabType,
      label: "Repositories",
      icon: Settings,
      count: null,
    },
    {
      id: "commits" as TabType,
      label: "Commits",
      icon: GitCommit,
      count: currentRepository ? commits.length : null,
    },
    {
      id: "pullrequests" as TabType,
      label: "Pull Requests",
      icon: GitPullRequest,
      count: currentRepository ? pullRequests.length : null,
    },
    {
      id: "systemprompts" as TabType,
      label: "System Prompts",
      icon: Settings,
      count: null,
    },
    {
      id: "workflows" as TabType,
      label: "Workflows",
      icon: Workflow,
      count: null,
    },
  ];

  return (
    <nav className={styles.navigation}>
      <div className={styles.navigationContainer}>
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
                <Icon className={styles.tabIcon} />
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
