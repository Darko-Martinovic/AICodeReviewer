import React from "react";
import { CheckCircle, Clock, AlertCircle, Loader2 } from "lucide-react";
import styles from "./ProgressIndicator.module.css";

export interface ProgressStep {
  id: string;
  name: string;
  description?: string;
  status: "pending" | "running" | "completed" | "failed" | "skipped";
  duration?: number;
  error?: string;
}

interface ProgressIndicatorProps {
  steps: ProgressStep[];
  title?: string;
  compact?: boolean;
}

export const ProgressIndicator: React.FC<ProgressIndicatorProps> = ({
  steps,
  title,
  compact = false,
}) => {
  const getStepIcon = (status: ProgressStep["status"]) => {
    switch (status) {
      case "completed":
        return <CheckCircle className={styles.iconCompleted} />;
      case "running":
        return (
          <Loader2 className={`${styles.iconRunning} ${styles.spinning}`} />
        );
      case "failed":
        return <AlertCircle className={styles.iconFailed} />;
      case "skipped":
        return <Clock className={styles.iconSkipped} />;
      default:
        return <Clock className={styles.iconPending} />;
    }
  };

  const getStepClass = (status: ProgressStep["status"]) => {
    switch (status) {
      case "completed":
        return styles.stepCompleted;
      case "running":
        return styles.stepRunning;
      case "failed":
        return styles.stepFailed;
      case "skipped":
        return styles.stepSkipped;
      default:
        return styles.stepPending;
    }
  };

  const formatDuration = (duration?: number) => {
    if (!duration) return "";
    if (duration < 1000) return `${duration}ms`;
    return `${(duration / 1000).toFixed(1)}s`;
  };

  if (compact) {
    const completedSteps = steps.filter((s) => s.status === "completed").length;
    const totalSteps = steps.length;
    const currentStepInfo = steps.find((s) => s.status === "running");

    return (
      <div className={styles.compactProgress}>
        <div className={styles.compactHeader}>
          <span className={styles.compactTitle}>
            {currentStepInfo ? currentStepInfo.name : title || "Processing"}
          </span>
          <span className={styles.compactCounter}>
            {completedSteps}/{totalSteps}
          </span>
        </div>
        <div className={styles.progressBar}>
          <div
            className={styles.progressFill}
            style={{ width: `${(completedSteps / totalSteps) * 100}%` }}
          />
        </div>
      </div>
    );
  }

  return (
    <div className={styles.progressContainer}>
      {title && <h3 className={styles.progressTitle}>{title}</h3>}

      <div className={styles.stepsList}>
        {steps.map((step, index) => (
          <div
            key={step.id}
            className={`${styles.step} ${getStepClass(step.status)}`}
          >
            <div className={styles.stepIcon}>{getStepIcon(step.status)}</div>

            <div className={styles.stepContent}>
              <div className={styles.stepHeader}>
                <span className={styles.stepName}>{step.name}</span>
                {step.duration && (
                  <span className={styles.stepDuration}>
                    {formatDuration(step.duration)}
                  </span>
                )}
              </div>

              {step.description && (
                <p className={styles.stepDescription}>{step.description}</p>
              )}

              {step.error && <p className={styles.stepError}>{step.error}</p>}
            </div>

            {index < steps.length - 1 && (
              <div className={styles.stepConnector} />
            )}
          </div>
        ))}
      </div>
    </div>
  );
};

export default ProgressIndicator;
