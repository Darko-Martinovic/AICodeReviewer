import React, { useEffect, useState, useCallback } from "react";
import { X, Clock, CheckCircle, Archive } from "lucide-react";
import ProgressIndicator from "./ProgressIndicator";
import type { ProgressStep } from "./ProgressIndicator";
import { cacheApi } from "../services/api";
import styles from "./ReviewProgressModal.module.css";

interface ReviewProgressModalProps {
  isOpen: boolean;
  onClose: () => void;
  reviewType: "commit" | "pullrequest";
  reviewId: string | number;
  title: string;
  forceCompleted?: boolean; // External control for completion state
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
  forceCompleted = false,
}) => {
  const [steps, setSteps] = useState<ProgressStep[]>(
    reviewType === "commit" ? [...COMMIT_STEPS] : [...PR_STEPS]
  );
  const [startTime, setStartTime] = useState<Date | null>(null);
  const [isCompleted, setIsCompleted] = useState(false);
  const [isCached, setIsCached] = useState(false);

  const checkCacheStatus = useCallback(async () => {
    try {
      const response =
        reviewType === "commit"
          ? await cacheApi.hasCommitReview(reviewId as string)
          : await cacheApi.hasPullRequestReview(reviewId as number);

      setIsCached(response.data);
      console.log(
        `📋 Cache check for ${reviewType} ${reviewId}: ${
          response.data ? "CACHED" : "NOT CACHED"
        }`
      );
    } catch (error) {
      console.error("Error checking cache:", error);
      setIsCached(false);
    }
  }, [reviewType, reviewId]);

  const simulateRealProgress = useCallback(() => {
    // For real progress, we'll use more realistic timing
    const stepDurations =
      reviewType === "commit"
        ? [3000, 15000, 5000] // Commit: 3s, 15s, 5s (total ~23s)
        : [5000, 120000, 30000, 20000, 5000]; // PR: 5s, 2min, 30s, 20s, 5s (total ~3min)

    let currentIndex = 0;
    let startStepTime = Date.now();

    const processNextStep = () => {
      if (currentIndex >= stepDurations.length) {
        // All steps completed
        setSteps((prev) =>
          prev.map((step) => ({
            ...step,
            status: step.status === "running" ? "completed" : step.status,
          }))
        );

        // Only auto-complete for commits or when forced
        if (reviewType === "commit" || forceCompleted) {
          setIsCompleted(true);
        }
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

      startStepTime = Date.now();

      // Complete current step after duration
      setTimeout(() => {
        const actualDuration = Date.now() - startStepTime;

        setSteps((prev) =>
          prev.map((step, index) => ({
            ...step,
            status: index === currentIndex ? "completed" : step.status,
            duration: index === currentIndex ? actualDuration : step.duration,
          }))
        );

        currentIndex++;

        // Small delay before next step
        setTimeout(processNextStep, 500);
      }, stepDurations[currentIndex]);
    };

    // Start first step after small delay
    setTimeout(processNextStep, 500);
  }, [reviewType, forceCompleted]);

  useEffect(() => {
    if (isOpen && !startTime) {
      setStartTime(new Date());
      setIsCompleted(false);

      // Reset steps
      const initialSteps =
        reviewType === "commit" ? [...COMMIT_STEPS] : [...PR_STEPS];
      setSteps(initialSteps);

      // Check cache status first
      checkCacheStatus();

      // Start the progress simulation
      simulateRealProgress();
    }
  }, [isOpen, reviewType, startTime, checkCacheStatus, simulateRealProgress]);

  // Handle external completion signal
  useEffect(() => {
    if (forceCompleted && !isCompleted) {
      setIsCompleted(true);
    }
  }, [forceCompleted, isCompleted]);

  const getElapsedTime = () => {
    if (!startTime) return "0s";
    const elapsed = Date.now() - startTime.getTime();
    if (elapsed < 1000) return `${elapsed}ms`;
    return `${(elapsed / 1000).toFixed(1)}s`;
  };

  const getTotalEstimatedTime = () => {
    if (isCached) {
      return "~instant (cached)";
    }

    const totalMs = reviewType === "commit" ? 23000 : 180000; // 23s for commits, 3min for PRs
    const minutes = Math.floor(totalMs / 60000);
    const seconds = Math.floor((totalMs % 60000) / 1000);

    if (minutes > 0) {
      return `~${minutes}m ${seconds}s`;
    }
    return `~${seconds}s`;
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

          {isCached && (
            <div className={styles.cacheNotice}>
              <Archive className={styles.cacheIcon} />
              <div>
                <h4 className={styles.cacheTitle}>Using Cached Results</h4>
                <p className={styles.cacheText}>
                  This {reviewType === "commit" ? "commit" : "pull request"} has
                  been reviewed before. Results will be available instantly.
                </p>
              </div>
            </div>
          )}

          {isCompleted ? (
            <div className={styles.completionMessage}>
              <CheckCircle className={styles.completionIcon} />
              <div>
                <h3 className={styles.completionTitle}>
                  {isCached ? "Cached Results Retrieved!" : "Review Completed!"}
                </h3>
                <p className={styles.completionText}>
                  {isCached
                    ? `The cached ${
                        reviewType === "commit"
                          ? "commit review"
                          : "pull request workflow"
                      } results have been retrieved successfully.`
                    : `The ${
                        reviewType === "commit"
                          ? "commit review"
                          : "pull request workflow"
                      } has been completed successfully.`}
                  {reviewType === "pullrequest" &&
                    !isCached &&
                    " The actual AI review is still processing in the background - please wait for the results window to appear automatically."}
                </p>
              </div>
            </div>
          ) : (
            !isCached &&
            reviewType === "pullrequest" && (
              <div className={styles.waitingMessage}>
                <div className={styles.waitingText}>
                  <strong>Processing Pull Request Review...</strong>
                  <br />
                  This includes AI analysis, GitHub commenting, and JIRA
                  integration.
                  <br />
                  <em>Expected duration: ~3 minutes</em>
                </div>
              </div>
            )
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
