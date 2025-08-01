import React, { useState } from "react";
import {
  Play,
  Pause,
  CheckCircle,
  XCircle,
  GitPullRequest,
  Settings,
  Plus,
  ArrowRight,
  ArrowDown,
  Zap,
  MessageSquare,
  ExternalLink,
  ChevronDown,
  ChevronRight,
  Workflow,
} from "lucide-react";
import { Modal, Alert } from "./UI";
import styles from "./WorkflowManagerNew.module.css";

interface Integration {
  id: string;
  name: string;
  type: "source" | "processing" | "notification" | "storage";
  icon: React.ReactNode;
  status: "active" | "inactive" | "error";
  config: Record<string, unknown>;
  description: string;
}

interface WorkflowStep {
  id: string;
  integration: Integration;
  position: { x: number; y: number };
  connections: string[]; // IDs of connected steps
}

interface WorkflowDefinition {
  id: string;
  name: string;
  description: string;
  steps: WorkflowStep[];
  isActive: boolean;
  executionMode: "serial" | "parallel";
}

const WorkflowManager: React.FC = () => {
  const [showModal, setShowModal] = useState(false);
  const [expandedWorkflow, setExpandedWorkflow] = useState<string | null>(
    "main-workflow"
  );

  // Available integrations
  const availableIntegrations: Integration[] = [
    {
      id: "github",
      name: "GitHub",
      type: "source",
      icon: <GitPullRequest className="w-5 h-5" />,
      status: "active",
      config: { webhook: true, repository: "AICodeReviewer" },
      description: "Pull requests and commits from GitHub",
    },
    {
      id: "jira",
      name: "Jira",
      type: "processing",
      icon: <ExternalLink className="w-5 h-5" />,
      status: "active",
      config: { project: "AIREV", ticketCreation: true },
      description: "Create and update Jira tickets",
    },
    {
      id: "slack",
      name: "Slack",
      type: "notification",
      icon: <MessageSquare className="w-5 h-5" />,
      status: "active",
      config: { channel: "#code-reviews", mentions: true },
      description: "Send notifications to Slack channels",
    },
    {
      id: "ai-review",
      name: "AI Code Review",
      type: "processing",
      icon: <Zap className="w-5 h-5" />,
      status: "active",
      config: { model: "gpt-4", complexity: "detailed" },
      description: "AI-powered code analysis and review",
    },
  ];

  // Predefined workflows
  const workflows: WorkflowDefinition[] = [
    {
      id: "main-workflow",
      name: "PR Review Workflow",
      description: "Complete pull request review and notification pipeline",
      executionMode: "serial",
      isActive: true,
      steps: [
        {
          id: "step-1",
          integration: availableIntegrations[0], // GitHub
          position: { x: 0, y: 0 },
          connections: ["step-2"],
        },
        {
          id: "step-2",
          integration: availableIntegrations[3], // AI Review
          position: { x: 1, y: 0 },
          connections: ["step-3", "step-4"],
        },
        {
          id: "step-3",
          integration: availableIntegrations[1], // Jira
          position: { x: 2, y: 0 },
          connections: [],
        },
        {
          id: "step-4",
          integration: availableIntegrations[2], // Slack
          position: { x: 2, y: 1 },
          connections: [],
        },
      ],
    },
    {
      id: "notification-workflow",
      name: "Notification Only",
      description: "Simplified workflow for notifications",
      executionMode: "parallel",
      isActive: false,
      steps: [
        {
          id: "step-n1",
          integration: availableIntegrations[0], // GitHub
          position: { x: 0, y: 0 },
          connections: ["step-n2", "step-n3"],
        },
        {
          id: "step-n2",
          integration: availableIntegrations[1], // Jira
          position: { x: 1, y: 0 },
          connections: [],
        },
        {
          id: "step-n3",
          integration: availableIntegrations[2], // Slack
          position: { x: 1, y: 1 },
          connections: [],
        },
      ],
    },
  ];

  const getStatusColor = (status: Integration["status"]) => {
    switch (status) {
      case "active":
        return styles.statusActive;
      case "inactive":
        return styles.statusInactive;
      case "error":
        return styles.statusError;
    }
  };

  const getWorkflowStatusClass = (isActive: boolean) => {
    return isActive ? styles.statusActive : styles.statusInactive;
  };

  const getExecutionModeClass = (mode: "serial" | "parallel") => {
    return mode === "serial" ? styles.modeSerial : styles.modeParallel;
  };

  const getStatusIcon = (status: Integration["status"]) => {
    switch (status) {
      case "active":
        return <CheckCircle className="w-4 h-4 text-green-500" />;
      case "inactive":
        return <Pause className="w-4 h-4 text-gray-500" />;
      case "error":
        return <XCircle className="w-4 h-4 text-red-500" />;
    }
  };

  const handleConfigureIntegration = () => {
    setShowModal(true);
  };

  const handleAddIntegration = () => {
    // Placeholder for add integration functionality
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
  };

  const renderWorkflowStep = (
    step: WorkflowStep,
    isLast: boolean,
    workflow: WorkflowDefinition
  ) => {
    const { integration } = step;
    const hasMultipleConnections = step.connections.length > 1;

    return (
      <div key={step.id} className={styles.stepContainer}>
        {/* Integration Step */}
        <div className={styles.stepCard}>
          <div className={styles.stepCardInner}>
            <div className={styles.stepHeader}>
              {integration.icon}
              <span className={styles.stepName}>{integration.name}</span>
              {getStatusIcon(integration.status)}
            </div>
            <p className={styles.stepDescription}>{integration.description}</p>
            <div className={styles.stepFooter}>
              <span
                className={`${styles.stepStatus} ${getStatusColor(
                  integration.status
                )}`}
              >
                {integration.status}
              </span>
              <button
                onClick={() => handleConfigureIntegration()}
                className={styles.stepConfigButton}
              >
                <Settings className="w-4 h-4" />
              </button>
            </div>
          </div>
        </div>

        {/* Connection Arrow(s) */}
        {!isLast && (
          <div className={styles.connector}>
            {workflow.executionMode === "serial" ? (
              <ArrowRight className={styles.connectorIcon} />
            ) : hasMultipleConnections ? (
              <div className={styles.connectorBranch}>
                <ArrowRight className={styles.connectorIcon} />
                <ArrowDown className={styles.connectorBranchIcon} />
              </div>
            ) : (
              <ArrowRight className={styles.connectorIcon} />
            )}
            <span className={styles.connectorLabel}>
              {workflow.executionMode}
            </span>
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
            Integration Workflows
          </h2>
          <p className={styles.headerDescription}>
            Configure and manage integration flows between services
          </p>
        </div>
        <div className={styles.headerActions}>
          <button
            onClick={() => handleAddIntegration()}
            className={styles.btnPrimary}
          >
            <Plus className="w-4 h-4" />
            Add Integration
          </button>
          <button className={styles.btnSecondary}>
            <Settings className="w-4 h-4" />
            Workflow Settings
          </button>
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
                  </div>
                </div>
                <div className={styles.workflowMeta}>
                  <span
                    className={`${styles.statusBadge} ${getWorkflowStatusClass(
                      workflow.isActive
                    )}`}
                  >
                    {workflow.isActive ? "Active" : "Inactive"}
                  </span>
                  <span
                    className={`${
                      styles.executionModeBadge
                    } ${getExecutionModeClass(workflow.executionMode)}`}
                  >
                    {workflow.executionMode === "serial"
                      ? "Sequential"
                      : "Parallel"}
                  </span>
                  <button className={styles.actionButton}>
                    <Play className="w-4 h-4" />
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
                      workflow
                    )
                  )}
                </div>

                {/* Add Step Button */}
                <div className={styles.addStepSection}>
                  <button
                    onClick={() => handleAddIntegration()}
                    className={styles.addStepButton}
                  >
                    <Plus className="w-4 h-4" />
                    Add Integration Step
                  </button>
                </div>
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Available Integrations */}
      <div className={styles.integrationsPanel}>
        <h3 className={styles.integrationsPanelTitle}>
          Available Integrations
        </h3>
        <div className={styles.integrationsGrid}>
          {availableIntegrations.map((integration) => (
            <div
              key={integration.id}
              className={styles.integrationCard}
              onClick={() => handleConfigureIntegration()}
            >
              <div className={styles.integrationHeader}>
                {integration.icon}
                <span className={styles.integrationName}>
                  {integration.name}
                </span>
                {getStatusIcon(integration.status)}
              </div>
              <p className={styles.integrationDescription}>
                {integration.description}
              </p>
              <span
                className={`${styles.stepStatus} ${getStatusColor(
                  integration.status
                )}`}
              >
                {integration.status}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Modal for integration configuration */}
      <Modal
        isOpen={showModal}
        onClose={closeModal}
        title="Integration Configuration - Under Development"
        maxWidth="lg"
      >
        <div className={styles.modalContent}>
          <Alert
            type="info"
            title="Feature Under Development"
            message="Integration configuration interface is currently being developed. The visual workflow designer is ready for demonstration, but the backend integration and configuration capabilities are still in progress."
          />

          <div className={styles.modalSection}>
            <h4 className={styles.modalSectionTitle}>Planned Features:</h4>
            <ul className={styles.modalList}>
              <li>• Drag-and-drop workflow designer</li>
              <li>• Real-time integration testing</li>
              <li>• Custom webhook configuration</li>
              <li>• Conditional branching and error handling</li>
              <li>• Integration marketplace with pre-built connectors</li>
              <li>• Monitoring and logging for each integration step</li>
            </ul>
          </div>

          <div className={styles.modalActions}>
            <button
              onClick={closeModal}
              className={styles.modalSecondaryButton}
            >
              Close
            </button>
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
