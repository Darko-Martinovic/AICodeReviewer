import React from "react";
import { Github, RefreshCw, Sun, Moon } from "lucide-react";
import type { Repository } from "../services/api";
import { useTheme } from "../hooks/useThemeHook";
import styles from "./Header.module.css";

interface HeaderProps {
  currentRepository: Repository | null;
  onRefresh: () => void;
}

export const Header: React.FC<HeaderProps> = ({
  currentRepository,
  onRefresh,
}) => {
  const { theme, toggleTheme } = useTheme();

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
              onClick={toggleTheme}
              className={styles.themeButton}
              title={`Switch to ${theme === "light" ? "dark" : "light"} mode`}
            >
              {theme === "light" ? (
                <Moon className={styles.themeIcon} />
              ) : (
                <Sun className={styles.themeIcon} />
              )}
            </button>
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
