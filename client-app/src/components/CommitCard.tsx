import React, { useState } from "react";
import type { Commit } from "../services/api";
import {
  GitCommit,
  ExternalLink,
  Calendar,
  User,
  ChevronDown,
  ChevronUp,
} from "lucide-react";
import { ProgressIndicator } from "./ProgressIndicator";
import type { ProgressStep } from "./ProgressIndicator";
import styles from "./CommitCard.module.css";

interface CommitCardProps {
  commit: Commit;
  onReview: (sha: string) => void;
  onCollaborate?: (commit: Commit) => void;
  isReviewing: boolean;
}

const mockSteps: ProgressStep[] = [
  { id: "analyzing", name: "Analyzing", status: "pending" },
  { id: "reviewing", name: "AI Review", status: "pending" },
  { id: "finalizing", name: "Finalizing", status: "pending" },
];

export const CommitCard: React.FC<CommitCardProps> = ({
  commit,
  onReview,
  onCollaborate,
  isReviewing,
}) => {
  const [isExpanded, setIsExpanded] = useState(false);

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

  const formatCommitMessage = (message: string) => {
    // Split the message into title and body
    const lines = message.split("\n");
    const title = lines[0] || "";
    const body = lines
      .slice(1)
      .filter((line) => line.trim())
      .join("\n");

    return { title, body };
  };

  const { title, body } = formatCommitMessage(commit.message);
  const hasBody = body && body.length > 0;
  const shouldShowToggle = hasBody && body.length > 150; // Show toggle for long messages

  return (
    <div className={styles.card}>
      <div className={styles.header}>
        <div className={styles.content}>
          <div className={styles.titleSection}>
            <GitCommit className={styles.commitIcon} />
            <h3 className={styles.title}>{title}</h3>
          </div>

          {hasBody && (
            <div className={styles.messageBody}>
              <div
                className={`${styles.messageText} ${
                  shouldShowToggle && !isExpanded ? styles.messageCollapsed : ""
                }`}
              >
                <pre className={styles.messagePreformatted}>{body}</pre>
              </div>
              {shouldShowToggle && (
                <button
                  type="button"
                  onClick={() => setIsExpanded(!isExpanded)}
                  className={styles.expandButton}
                >
                  {isExpanded ? (
                    <>
                      <ChevronUp className={styles.expandIcon} />
                      Show less
                    </>
                  ) : (
                    <>
                      <ChevronDown className={styles.expandIcon} />
                      Show more
                    </>
                  )}
                </button>
              )}
            </div>
          )}

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
        {isReviewing ? (
          <ProgressIndicator steps={mockSteps} compact={true} />
        ) : (
          <div className={styles.actionButtons}>
            <button
              onClick={handleReview}
              disabled={isReviewing}
              className={styles.reviewButton}
            >
              Review Code
            </button>
            {onCollaborate && (
              <button
                onClick={() => onCollaborate(commit)}
                className={styles.collaborateButton}
                title="Start collaborative review session"
              >
                🚀 Collaborate
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
};
