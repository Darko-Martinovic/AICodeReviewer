import React from "react";
import type { Commit } from "../services/api";
import { GitCommit, ExternalLink, Calendar, User } from "lucide-react";
import styles from "./CommitCard.module.css";

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
    <div className={styles.card}>
      <div className={styles.header}>
        <div className={styles.content}>
          <div className={styles.titleSection}>
            <GitCommit className={styles.commitIcon} />
            <h3 className={styles.title}>{commit.message}</h3>
          </div>

          <div className={styles.metadata}>
            <div className={styles.metaItem}>
              <User className={styles.metaIcon} />
              <span>{commit.author}</span>
            </div>
            <div className={styles.metaItem}>
              <Calendar className={styles.metaIcon} />
              <span>{formatDate(commit.date)}</span>
            </div>
          </div>

          <div className={styles.commitInfo}>
            <code className={styles.shaCode}>{commit.sha.substring(0, 8)}</code>
            <a
              href={commit.htmlUrl}
              target="_blank"
              rel="noopener noreferrer"
              className={styles.externalLink}
            >
              <ExternalLink className={styles.externalLinkIcon} />
            </a>
          </div>
        </div>
      </div>

      <div className={styles.footer}>
        <button
          onClick={handleReview}
          disabled={isReviewing}
          className={styles.reviewButton}
        >
          {isReviewing && <div className={styles.spinner} />}
          {isReviewing ? "Reviewing..." : "Review Code"}
        </button>
      </div>
    </div>
  );
};
