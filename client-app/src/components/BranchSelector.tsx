import React from "react";
import { GitBranch, ChevronDown } from "lucide-react";
import styles from "./BranchSelector.module.css";

interface BranchSelectorProps {
  branches: string[];
  selectedBranch: string;
  onBranchSelect: (branch: string) => void;
  loading?: boolean;
}

export const BranchSelector: React.FC<BranchSelectorProps> = ({
  branches,
  selectedBranch,
  onBranchSelect,
  loading = false,
}) => {
  if (branches.length === 0 && !loading) {
    return null;
  }

  return (
    <div className={styles.container}>
      <div className={styles.selectorWrapper}>
        <GitBranch className={styles.icon} />
        <label htmlFor="branch-select" className={styles.label}>
          Branch:
        </label>
        <div className={styles.selectWrapper}>
          <select
            id="branch-select"
            value={selectedBranch}
            onChange={(e) => onBranchSelect(e.target.value)}
            disabled={loading}
            className={styles.select}
          >
            {branches.map((branch) => (
              <option key={branch} value={branch}>
                {branch}
              </option>
            ))}
          </select>
          <ChevronDown className={styles.chevron} />
        </div>
      </div>
      {loading && (
        <div className={styles.loading}>
          <span>Loading branches...</span>
        </div>
      )}
    </div>
  );
};
