import React from "react";
import type { Repository } from "../services/api";
import { GitBranch, ExternalLink, Lock, Unlock } from "lucide-react";
import styles from "./RepositoryCard.module.css";

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
      className={isSelected ? styles.cardSelected : styles.card}
      onClick={handleSelect}
    >
      <div className={styles.headerSection}>
        <div className={styles.contentContainer}>
          <div className={styles.titleSection}>
            <h3 className={styles.title}>{repository.name}</h3>
            {repository.private ? (
              <Lock className={styles.lockIcon} />
            ) : (
              <Unlock className={styles.unlockIcon} />
            )}
          </div>

          <p className={styles.fullName}>{repository.fullName}</p>

          {repository.description && (
            <p className={styles.description}>{repository.description}</p>
          )}

          <div className={styles.metadataSection}>
            <div className={styles.branchInfo}>
              <GitBranch className={styles.branchIcon} />
              <span>{repository.defaultBranch}</span>
            </div>
            <span
              className={
                repository.private ? styles.statusPrivate : styles.statusPublic
              }
            >
              {repository.private ? "Private" : "Public"}
            </span>
          </div>
        </div>

        <a
          href={repository.htmlUrl}
          target="_blank"
          rel="noopener noreferrer"
          className={styles.externalLink}
          onClick={(e) => e.stopPropagation()}
        >
          <ExternalLink className={styles.externalLinkIcon} />
        </a>
      </div>
    </div>
  );
};
