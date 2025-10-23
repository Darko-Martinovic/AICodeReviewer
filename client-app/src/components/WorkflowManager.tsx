import React, { useState } from "react";
import {
  Play,
  CheckCircle,
  XCircle,
  GitPullRequest,
  GitCommit,
  ArrowRight,
  Zap,
  MessageSquare,
  ExternalLink,
  ChevronDown,
  ChevronRight,
  Workflow,
  ToggleLeft,
  ToggleRight,
} from "lucide-react";
import { Modal, Alert } from "./UI";
import styles from "./WorkflowManager.module.css";

interface WorkflowStep {
  id: string;
  name: string;
  description: string;
  icon: React.ReactNode;
  type: "source" | "process" | "action" | "notification";
  status: "pending" | "running" | "completed" | "error";
  enabled: boolean;
}

interface WorkflowDefinition {
  id: string;
  name: string;
  description: string;
  steps: WorkflowStep[];
  enabled: boolean;
  executionMode: "serial" | "parallel";
  fileName?: string;
}

const WorkflowManager: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [expandedWorkflow, setExpandedWorkflow] = useState<string | null>(
    "pr-review"
  );
  const [isExecuting, setIsExecuting] = useState(false);
  const [executingStep, setExecutingStep] = useState<string | null>(null);

  // Predefined workflows with enable/disable functionality
  const [workflows, setWorkflows] = useState<WorkflowDefinition[]>([
    {
      id: "pr-review",
      name: "PR Review Workflow",
      description:
        "Complete pull request review with AI analysis and team notifications",
      enabled: true,
      executionMode: "serial",
      fileName: "pull-request-workflow.json",
      steps: [
        {
          id: "github-pr",
          name: "GitHub PR",
          description: "Fetch PR details and changes",
          icon: <GitPullRequest className="w-5 h-5" />,
          type: "source",
          status: "pending",
          enabled: true,
        },
        {
          id: "ai-review",
          name: "AI Review",
          description: "Analyze code with AI",
          icon: <Zap className="w-5 h-5" />,
          type: "process",
          status: "pending",
          enabled: true,
        },
        {
          id: "github-comment",
          name: "GitHub Comment",
          description: "Post review comments to PR",
          icon: <MessageSquare className="w-5 h-5" />,
          type: "action",
          status: "pending",
          enabled: true,
        },
        {
          id: "jira-ticket",
          name: "JIRA Ticket",
          description: "Create or update JIRA ticket",
          icon: <ExternalLink className="w-5 h-5" />,
          type: "action",
          status: "pending",
          enabled: true,
        },
        {
          id: "slack-notify",
          name: "Slack Notification",
          description: "Send notification to Slack",
          icon: <MessageSquare className="w-5 h-5" />,
          type: "notification",
          status: "pending",
          enabled: true,
        },
      ],
    },
    {
      id: "commit-review",
      name: "Commit Review Workflow",
      description:
        "Simplified commit review with AI analysis and Slack notifications",
      enabled: true,
      fileName: "commit-workflow.json",
      executionMode: "serial",
      steps: [
        {
          id: "github-commit",
          name: "GitHub Commit",
          description: "Fetch commit details and changes",
          icon: <GitCommit className="w-5 h-5" />,
          type: "source",
          status: "pending",
          enabled: true,
        },
        {
          id: "ai-review",
          name: "AI Review",
          description: "Analyze code with AI",
          icon: <Zap className="w-5 h-5" />,
          type: "process",
          status: "pending",
          enabled: true,
        },
        {
          id: "slack-notification",
          name: "Slack Notification",
          description: "Send notification to Slack",
          icon: <MessageSquare className="w-5 h-5" />,
          type: "notification",
          status: "pending",
          enabled: true,
        },
      ],
    },
  ]);

  const toggleStepEnabled = (workflowId: string, stepId: string) => {
    setWorkflows((prevWorkflows) =>
      prevWorkflows.map((workflow) =>
        workflow.id === workflowId
          ? {
              ...workflow,
              steps: workflow.steps.map((step) =>
                step.id === stepId ? { ...step, enabled: !step.enabled } : step
              ),
            }
          : workflow
      )
    );
  };

  const toggleWorkflowEnabled = (workflowId: string) => {
    setWorkflows((prevWorkflows) =>
      prevWorkflows.map((workflow) =>
        workflow.id === workflowId
          ? { ...workflow, enabled: !workflow.enabled }
          : workflow
      )
    );
  };

  const executeWorkflow = async (
    workflowId: string,
    prNumber?: number,
    commitSha?: string
  ) => {
    setIsExecuting(true);
    const workflow = workflows.find((w) => w.id === workflowId);

    if (!workflow || !workflow.enabled) {
      setIsExecuting(false);
      return;
    }

    try {
      // Get only enabled steps
      const enabledSteps = workflow.steps.filter((step) => step.enabled);

      // Execute steps sequentially
      for (const step of enabledSteps) {
        setExecutingStep(step.id);

        // Update step status to running
        setWorkflows((prevWorkflows) =>
          prevWorkflows.map((w) =>
            w.id === workflowId
              ? {
                  ...w,
                  steps: w.steps.map((s) =>
                    s.id === step.id ? { ...s, status: "running" as const } : s
                  ),
                }
              : w
          )
        );

        // Simulate step execution
        await new Promise((resolve) => setTimeout(resolve, 1500));

        // Update step status to completed
        setWorkflows((prevWorkflows) =>
          prevWorkflows.map((w) =>
            w.id === workflowId
              ? {
                  ...w,
                  steps: w.steps.map((s) =>
                    s.id === step.id
                      ? { ...s, status: "completed" as const }
                      : s
                  ),
                }
              : w
          )
        );
      }

      // Call the actual API endpoint based on workflow type
      if (workflowId === "pr-review" && prNumber) {
        const response = await fetch(
          `/api/workflows/execute/pullrequest/${prNumber}`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
          }
        );
        if (!response.ok) {
          throw new Error(
            `Failed to execute PR workflow: ${response.statusText}`
          );
        }
      } else if (workflowId === "commit-review" && commitSha) {
        const response = await fetch(
          `/api/workflows/execute/commit/${commitSha}`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
          }
        );
        if (!response.ok) {
          throw new Error(
            `Failed to execute commit workflow: ${response.statusText}`
          );
        }
      }
    } catch (error) {
      console.error("Workflow execution failed:", error);

      // Mark current step as error
      if (executingStep) {
        setWorkflows((prevWorkflows) =>
          prevWorkflows.map((w) =>
            w.id === workflowId
              ? {
                  ...w,
                  steps: w.steps.map((s) =>
                    s.id === executingStep
                      ? { ...s, status: "error" as const }
                      : s
                  ),
                }
              : w
          )
        );
      }
    } finally {
      setIsExecuting(false);
      setExecutingStep(null);
    }
  };

  const resetWorkflowStatus = (workflowId: string) => {
    setWorkflows((prevWorkflows) =>
      prevWorkflows.map((w) =>
        w.id === workflowId
          ? {
              ...w,
              steps: w.steps.map((s) => ({ ...s, status: "pending" as const })),
            }
          : w
      )
    );
  };

  const getStatusColor = (status: WorkflowStep["status"]) => {
    switch (status) {
      case "completed":
        return styles.statusCompleted || "text-green-500";
      case "running":
        return styles.statusRunning || "text-blue-500";
      case "error":
        return styles.statusError || "text-red-500";
      default:
        return styles.statusPending || "text-gray-500";
    }
  };

  const getStatusIcon = (status: WorkflowStep["status"]) => {
    switch (status) {
      case "completed":
        return <CheckCircle className="w-4 h-4 text-green-500" />;
      case "running":
        return (
          <div className="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin" />
        );
      case "error":
        return <XCircle className="w-4 h-4 text-red-500" />;
      default:
        return (
          <div className="w-4 h-4 rounded-full border-2 border-gray-300" />
        );
    }
  };

  const closeModal = () => {
    setShowModal(false);
  };

  const renderWorkflowStep = (
    step: WorkflowStep,
    isLast: boolean,
    workflowId: string
  ) => {
    return (
      <div key={step.id} className={styles.stepContainer}>
        {/* Integration Step */}
        <div
          className={`${styles.stepCard} ${
            !step.enabled ? styles.stepDisabled : ""
          }`}
        >
          <div className={styles.stepCardInner}>
            <div className={styles.stepHeader}>
              {step.icon}
              <span className={styles.stepName}>{step.name}</span>
              {getStatusIcon(step.status)}
            </div>
            <p className={styles.stepDescription}>{step.description}</p>
            <div className={styles.stepFooter}>
              <span
                className={`${styles.stepStatus} ${getStatusColor(
                  step.status
                )}`}
              >
                {step.status}
              </span>
              <button
                onClick={() => toggleStepEnabled(workflowId, step.id)}
                className={styles.stepToggleButton}
                title={step.enabled ? "Disable step" : "Enable step"}
              >
                {step.enabled ? (
                  <ToggleRight className="w-5 h-5 text-green-500" />
                ) : (
                  <ToggleLeft className="w-5 h-5 text-gray-400" />
                )}
              </button>
            </div>
          </div>
        </div>

        {/* Connection Arrow */}
        {!isLast && (
          <div className={styles.connector}>
            <ArrowRight className={styles.connectorIcon} />
          </div>
        )}
      </div>
    );
  };

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h2 className={styles.headerTitle}>
            <Workflow className="w-7 h-7" />
            Workflow Management
          </h2>
          <p className={styles.headerDescription}>
            Configure and execute automated workflows for pull requests and
            commits
          </p>
        </div>
      </div>

      {/* Workflows List */}
      <div className={styles.workflowList}>
        {workflows.map((workflow) => (
          <div key={workflow.id} className={styles.workflowCard}>
            {/* Workflow Header */}
            <div className={styles.workflowHeader}>
              <div className={styles.workflowHeaderContent}>
                <div className={styles.workflowInfo}>
                  <button
                    onClick={() =>
                      setExpandedWorkflow(
                        expandedWorkflow === workflow.id ? null : workflow.id
                      )
                    }
                    className={styles.expandButton}
                  >
                    {expandedWorkflow === workflow.id ? (
                      <ChevronDown className="w-5 h-5" />
                    ) : (
                      <ChevronRight className="w-5 h-5" />
                    )}
                  </button>
                  <div>
                    <h3 className={styles.workflowTitle}>{workflow.name}</h3>
                    <p className={styles.workflowDescription}>
                      {workflow.description}
                    </p>
                    <p className={styles.workflowFileName}>
                      File: {workflow.fileName}
                    </p>
                  </div>
                </div>
                <div className={styles.workflowMeta}>
                  <button
                    onClick={() => toggleWorkflowEnabled(workflow.id)}
                    className={styles.workflowToggleButton}
                    title={
                      workflow.enabled ? "Disable workflow" : "Enable workflow"
                    }
                  >
                    {workflow.enabled ? (
                      <ToggleRight className="w-6 h-6 text-green-500" />
                    ) : (
                      <ToggleLeft className="w-6 h-6 text-gray-400" />
                    )}
                  </button>
                  <button
                    className={styles.actionButton}
                    onClick={() => {
                      if (workflow.id === "pr-review") {
                        // For demo purposes, use PR #1
                        executeWorkflow(workflow.id, 1);
                      } else if (workflow.id === "commit-review") {
                        // For demo purposes, use a sample commit SHA
                        executeWorkflow(workflow.id, undefined, "abc123");
                      }
                    }}
                    disabled={!workflow.enabled || isExecuting}
                    title={
                      workflow.enabled
                        ? "Execute workflow"
                        : "Workflow is disabled"
                    }
                  >
                    <Play className="w-4 h-4" />
                  </button>
                  <button
                    className={styles.actionButton}
                    onClick={() => resetWorkflowStatus(workflow.id)}
                    title="Reset workflow status"
                  >
                    Reset
                  </button>
                </div>
              </div>
            </div>

            {/* Workflow Steps */}
            {expandedWorkflow === workflow.id && (
              <div className={styles.workflowContent}>
                <div className={styles.workflowSteps}>
                  {workflow.steps.map((step, index) =>
                    renderWorkflowStep(
                      step,
                      index === workflow.steps.length - 1,
                      workflow.id
                    )
                  )}
                </div>
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Modal for configuration */}
      <Modal
        isOpen={showModal}
        onClose={closeModal}
        title="Workflow Configuration"
        maxWidth="lg"
      >
        <div className={styles.modalContent}>
          <Alert
            type="info"
            title="Enhanced Workflow Management"
            message="The workflow system now supports enable/disable toggles for individual steps and complete workflows. Only enabled steps will be executed during workflow runs."
          />

          <div className={styles.modalSection}>
            <h4 className={styles.modalSectionTitle}>Available Features:</h4>
            <ul className={styles.modalList}>
              <li>• Enable/disable individual workflow steps</li>
              <li>• Enable/disable entire workflows</li>
              <li>• Real-time status updates during execution</li>
              <li>• Integration with backend workflow API</li>
              <li>• State-aware execution based on enabled components</li>
              <li>• Visual feedback for disabled components</li>
            </ul>
          </div>

          <div className={styles.modalActions}>
            <button onClick={closeModal} className={styles.modalPrimaryButton}>
              Got it!
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default WorkflowManager;
