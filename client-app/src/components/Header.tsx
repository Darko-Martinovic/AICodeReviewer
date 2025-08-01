import React from "react";
import { Github, RefreshCw } from "lucide-react";
import type { Repository } from "../services/api";
import styles from "./Header.module.css";

interface HeaderProps {
  currentRepository: Repository | null;
  onRefresh: () => void;
}

export const Header: React.FC<HeaderProps> = ({
  currentRepository,
  onRefresh,
}) => {
  return (
    <header className={styles.header}>
      <div className={styles.headerContainer}>
        <div className={styles.headerContent}>
          <div className={styles.logoSection}>
            <Github className={styles.logoIcon} />
            <h1 className={styles.logoTitle}>AI Code Reviewer</h1>
          </div>

          <div className={styles.headerActions}>
            {currentRepository && (
              <div className={styles.currentRepositoryIndicator}>
                <div className={styles.statusDot}></div>
                <div className={styles.repositoryInfo}>
                  <span className={styles.repositoryLabel}>Current:</span>{" "}
                  <span className={styles.repositoryName}>
                    {currentRepository.fullName}
                  </span>
                </div>
              </div>
            )}
            <button
              onClick={onRefresh}
              className={styles.refreshButton}
              title="Refresh"
            >
              <RefreshCw className={styles.refreshIcon} />
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};

export default Header;
