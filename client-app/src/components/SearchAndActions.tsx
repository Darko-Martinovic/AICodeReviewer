import React from "react";
import { Search, Plus } from "lucide-react";
import styles from "./SearchAndActions.module.css";

type TabType =
  | "repositories"
  | "commits"
  | "pullrequests"
  | "systemprompts"
  | "workflows"
  | "repositoryfilters";

interface SearchAndActionsProps {
  searchQuery: string;
  onSearchChange: (query: string) => void;
  activeTab: TabType;
  onAddRepository: () => void;
}

export const SearchAndActions: React.FC<SearchAndActionsProps> = ({
  searchQuery,
  onSearchChange,
  activeTab,
  onAddRepository,
}) => {
  const getSearchPlaceholder = () => {
    switch (activeTab) {
      case "repositories":
        return "Search repositories...";
      case "commits":
        return "Search commits...";
      case "pullrequests":
        return "Search pull requests...";
      case "systemprompts":
        return "Search prompts...";
      case "workflows":
        return "Search workflows...";
      case "repositoryfilters":
        return "Search filter patterns...";
      default:
        return "Search...";
    }
  };

  const showAddRepositoryButton = activeTab === "repositories";
  const showSearchBox =
    activeTab !== "systemprompts" &&
    activeTab !== "workflows" &&
    activeTab !== "repositoryfilters";

  return (
    <div className={styles.searchAndActions}>
      {showSearchBox && (
        <div className={styles.searchContainer}>
          <Search className={styles.searchIcon} />
          <input
            type="text"
            placeholder={getSearchPlaceholder()}
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
            className={styles.searchInput}
          />
        </div>
      )}

      {showAddRepositoryButton && (
        <div className={styles.actionsContainer}>
          <button onClick={onAddRepository} className={styles.actionButton}>
            <Plus className={styles.actionButtonIcon} />
            Add Repository
          </button>
        </div>
      )}
    </div>
  );
};

export default SearchAndActions;
