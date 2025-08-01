import React from "react";
import type { PullRequest } from "../services/api";
import {
  GitPullRequest,
  ExternalLink,
  Calendar,
  User,
  GitBranch,
} from "lucide-react";
import styles from "./PullRequestCard.module.css";

interface PullRequestCardProps {
  pullRequest: PullRequest;
  onReview: (number: number) => void;
  isReviewing: boolean;
}

export const PullRequestCard: React.FC<PullRequestCardProps> = ({
  pullRequest,
  onReview,
  isReviewing,
}) => {
  const handleReview = () => {
    onReview(pullRequest.number);
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

  const getStateColor = (state: string) => {
    switch (state.toLowerCase()) {
      case "open":
        return styles.stateOpen;
      case "closed":
        return styles.stateClosed;
      case "merged":
        return styles.stateMerged;
      default:
        return styles.stateDefault;
    }
  };

  return (
    <div className={styles.card}>
      <div className={styles.headerSection}>
        <div className={styles.contentContainer}>
          <div className={styles.titleSection}>
            <GitPullRequest className={styles.prIcon} />
            <h3 className={styles.title}>{pullRequest.title}</h3>
            <span className={getStateColor(pullRequest.state)}>
              {pullRequest.state}
            </span>
          </div>

          <div className={styles.metadataSection}>
            <div className={styles.metadataItem}>
              <span>#{pullRequest.number}</span>
            </div>
            <div className={styles.metadataItem}>
              <User className={styles.metadataIcon} />
              <span>{pullRequest.author}</span>
            </div>
            <div className={styles.metadataItem}>
              <Calendar className={styles.metadataIcon} />
              <span>{formatDate(pullRequest.createdAt)}</span>
            </div>
          </div>

          <div className={styles.branchSection}>
            <GitBranch className={styles.branchIcon} />
            <span className={styles.branchText}>
              <code className={styles.branchCode}>
                {pullRequest.headBranch}
              </code>
              <span className={styles.branchArrow}>→</span>
              <code className={styles.branchCode}>
                {pullRequest.baseBranch}
              </code>
            </span>
          </div>

          {pullRequest.body && (
            <p className={styles.bodyText}>{pullRequest.body}</p>
          )}

          <div className={styles.linkSection}>
            <a
              href={pullRequest.htmlUrl}
              target="_blank"
              rel="noopener noreferrer"
              className={styles.viewPrLink}
            >
              <ExternalLink className={styles.linkIcon} />
            </a>
          </div>
        </div>
      </div>

      <div className={styles.buttonSection}>
        <button
          onClick={handleReview}
          disabled={isReviewing || pullRequest.state.toLowerCase() !== "open"}
          className={`${styles.reviewButton} ${
            isReviewing || pullRequest.state.toLowerCase() !== "open"
              ? styles.reviewButtonDisabled
              : ""
          }`}
        >
          {isReviewing && <div className={styles.spinner} />}
          {isReviewing ? "Reviewing..." : "Review PR"}
        </button>
      </div>
    </div>
  );
};
