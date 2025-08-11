import React, { useEffect, useState, useCallback } from "react";
import { X, Clock, CheckCircle } from "lucide-react";
import ProgressIndicator from "./ProgressIndicator";
import type { ProgressStep } from "./ProgressIndicator";
import styles from "./ReviewProgressModal.module.css";

interface ReviewProgressModalProps {
  isOpen: boolean;
  onClose: () => void;
  reviewType: "commit" | "pullrequest";
  reviewId: string | number;
  title: string;
}

const COMMIT_STEPS: ProgressStep[] = [
  {
    id: "fetch",
    name: "Fetching Code Changes",
    description: "Retrieving commit files and diff information",
    status: "pending",
  },
  {
    id: "ai_analysis",
    name: "AI Code Analysis",
    description: "Running AI-powered code review analysis",
    status: "pending",
  },
  {
    id: "generate_report",
    name: "Generating Report",
    description: "Creating detailed review report with findings",
    status: "pending",
  },
];

const PR_STEPS: ProgressStep[] = [
  {
    id: "fetch_pr",
    name: "Fetching Pull Request",
    description: "Retrieving PR files and change information",
    status: "pending",
  },
  {
    id: "ai_review",
    name: "AI Code Review",
    description: "Analyzing code changes with AI review engine",
    status: "pending",
  },
  {
    id: "github_comment",
    name: "Post GitHub Comment",
    description: "Adding review comments to the pull request",
    status: "pending",
  },
  {
    id: "jira_integration",
    name: "JIRA Integration",
    description: "Creating or updating JIRA tickets based on findings",
    status: "pending",
  },
  {
    id: "finalize",
    name: "Finalizing Review",
    description: "Completing the review workflow",
    status: "pending",
  },
];

export const ReviewProgressModal: React.FC<ReviewProgressModalProps> = ({
  isOpen,
  onClose,
  reviewType,
  reviewId,
  title,
}) => {
  const [steps, setSteps] = useState<ProgressStep[]>(
    reviewType === "commit" ? [...COMMIT_STEPS] : [...PR_STEPS]
  );
  const [startTime, setStartTime] = useState<Date | null>(null);
  const [isCompleted, setIsCompleted] = useState(false);

  const simulateProgress = useCallback(() => {
    const stepDurations =
      reviewType === "commit"
        ? [2000, 8000, 3000] // Commit steps: 2s, 8s, 3s
        : [2000, 10000, 4000, 5000, 2000]; // PR steps: 2s, 10s, 4s, 5s, 2s

    let currentIndex = 0;

    const processNextStep = () => {
      if (currentIndex >= stepDurations.length) {
        // All steps completed
        setSteps((prev) =>
          prev.map((step) => ({
            ...step,
            status: step.status === "running" ? "completed" : step.status,
          }))
        );
        setIsCompleted(true);
        return;
      }

      // Mark current step as running
      setSteps((prev) =>
        prev.map((step, index) => ({
          ...step,
          status:
            index === currentIndex
              ? "running"
              : index < currentIndex
              ? "completed"
              : "pending",
        }))
      );

      // Complete current step after duration
      setTimeout(() => {
        setSteps((prev) =>
          prev.map((step, index) => ({
            ...step,
            status: index === currentIndex ? "completed" : step.status,
            duration:
              index === currentIndex
                ? stepDurations[currentIndex]
                : step.duration,
          }))
        );

        currentIndex++;

        // Small delay before next step
        setTimeout(processNextStep, 500);
      }, stepDurations[currentIndex]);
    };

    // Start first step after small delay
    setTimeout(processNextStep, 500);
  }, [reviewType]);

  useEffect(() => {
    if (isOpen && !startTime) {
      setStartTime(new Date());
      setIsCompleted(false);

      // Reset steps
      const initialSteps =
        reviewType === "commit" ? [...COMMIT_STEPS] : [...PR_STEPS];
      setSteps(initialSteps);

      // Start the simulated progress
      simulateProgress();
    }
  }, [isOpen, reviewType, startTime, simulateProgress]);

  const getElapsedTime = () => {
    if (!startTime) return "0s";
    const elapsed = Date.now() - startTime.getTime();
    if (elapsed < 1000) return `${elapsed}ms`;
    return `${(elapsed / 1000).toFixed(1)}s`;
  };

  const getTotalEstimatedTime = () => {
    const totalMs = reviewType === "commit" ? 13000 : 23000;
    return `~${totalMs / 1000}s`;
  };

  if (!isOpen) return null;

  return (
    <div className={styles.overlay}>
      <div className={styles.modal}>
        <div className={styles.header}>
          <div className={styles.titleSection}>
            <div className={styles.titleContainer}>
              <h2 className={styles.title}>{title}</h2>
              <div className={styles.subtitle}>
                {reviewType === "commit"
                  ? `Commit ${reviewId}`
                  : `Pull Request #${reviewId}`}
              </div>
            </div>
            <div className={styles.timing}>
              <div className={styles.timeDisplay}>
                <Clock className={styles.timeIcon} />
                <span>{getElapsedTime()}</span>
                <span className={styles.estimatedTime}>
                  / {getTotalEstimatedTime()}
                </span>
              </div>
            </div>
          </div>

          <button
            onClick={onClose}
            className={styles.closeButton}
            disabled={!isCompleted}
            title={isCompleted ? "Close" : "Please wait for review to complete"}
          >
            <X className={styles.closeIcon} />
          </button>
        </div>

        <div className={styles.content}>
          <ProgressIndicator
            steps={steps}
            title={
              reviewType === "commit"
                ? "Commit Review Progress"
                : "Pull Request Workflow"
            }
          />

          {isCompleted && (
            <div className={styles.completionMessage}>
              <CheckCircle className={styles.completionIcon} />
              <div>
                <h3 className={styles.completionTitle}>Review Completed!</h3>
                <p className={styles.completionText}>
                  The{" "}
                  {reviewType === "commit"
                    ? "commit review"
                    : "pull request workflow"}{" "}
                  has been completed successfully.
                  {reviewType === "pullrequest" &&
                    " Check GitHub for the review comment and JIRA for any created tickets."}
                </p>
              </div>
            </div>
          )}
        </div>

        {isCompleted && (
          <div className={styles.footer}>
            <button onClick={onClose} className={styles.doneButton}>
              Done
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default ReviewProgressModal;
