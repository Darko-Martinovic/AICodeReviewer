import React, { useState } from "react";
import {
  Play,
  Pause,
  CheckCircle,
  XCircle,
  Clock,
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

interface Integration {
  id: string;
  name: string;
  type: "source" | "processing" | "notification" | "storage";
  icon: React.ReactNode;
  status: "active" | "inactive" | "error";
  config: Record<string, any>;
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
  const [selectedWorkflow, setSelectedWorkflow] =
    useState<WorkflowDefinition | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [showAddIntegration, setShowAddIntegration] = useState(false);
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
        return "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200";
      case "inactive":
        return "bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200";
      case "error":
        return "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200";
    }
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

  const handleConfigureIntegration = (integration: Integration) => {
    setSelectedWorkflow(null);
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setSelectedWorkflow(null);
  };

  const renderWorkflowStep = (
    step: WorkflowStep,
    isLast: boolean,
    workflow: WorkflowDefinition
  ) => {
    const { integration } = step;
    const hasMultipleConnections = step.connections.length > 1;

    return (
      <div key={step.id} className="flex items-center">
        {/* Integration Step */}
        <div className="relative group">
          <div className="bg-white dark:bg-gray-800 border-2 border-gray-200 dark:border-gray-700 rounded-lg p-4 hover:border-blue-300 dark:hover:border-blue-600 transition-colors min-w-[160px]">
            <div className="flex items-center gap-2 mb-2">
              {integration.icon}
              <span className="font-medium text-gray-900 dark:text-white">
                {integration.name}
              </span>
              {getStatusIcon(integration.status)}
            </div>
            <p className="text-xs text-gray-600 dark:text-gray-400 mb-2">
              {integration.description}
            </p>
            <div className="flex items-center justify-between">
              <span
                className={`px-2 py-1 text-xs rounded-full ${getStatusColor(
                  integration.status
                )}`}
              >
                {integration.status}
              </span>
              <button
                onClick={() => handleConfigureIntegration(integration)}
                className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                <Settings className="w-4 h-4" />
              </button>
            </div>
          </div>
        </div>

        {/* Connection Arrow(s) */}
        {!isLast && (
          <div className="flex flex-col items-center mx-4">
            {workflow.executionMode === "serial" ? (
              <ArrowRight className="w-6 h-6 text-blue-500" />
            ) : hasMultipleConnections ? (
              <div className="flex flex-col items-center">
                <ArrowRight className="w-6 h-6 text-blue-500" />
                <ArrowDown className="w-6 h-6 text-blue-500 -mt-2" />
              </div>
            ) : (
              <ArrowRight className="w-6 h-6 text-blue-500" />
            )}
            <span className="text-xs text-gray-500 mt-1">
              {workflow.executionMode}
            </span>
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <Workflow className="w-7 h-7" />
            Integration Workflows
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Configure and manage integration flows between services
          </p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setShowAddIntegration(true)}
            className="btn-primary flex items-center gap-2"
          >
            <Plus className="w-4 h-4" />
            Add Integration
          </button>
          <button className="btn-secondary flex items-center gap-2">
            <Settings className="w-4 h-4" />
            Workflow Settings
          </button>
        </div>
      </div>

      {/* Workflows List */}
      <div className="space-y-4">
        {workflows.map((workflow) => (
          <div
            key={workflow.id}
            className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm"
          >
            {/* Workflow Header */}
            <div className="p-4 border-b border-gray-200 dark:border-gray-700">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <button
                    onClick={() =>
                      setExpandedWorkflow(
                        expandedWorkflow === workflow.id ? null : workflow.id
                      )
                    }
                    className="text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
                  >
                    {expandedWorkflow === workflow.id ? (
                      <ChevronDown className="w-5 h-5" />
                    ) : (
                      <ChevronRight className="w-5 h-5" />
                    )}
                  </button>
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                      {workflow.name}
                    </h3>
                    <p className="text-sm text-gray-600 dark:text-gray-400">
                      {workflow.description}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <span
                    className={`px-3 py-1 text-sm rounded-full ${
                      workflow.isActive
                        ? "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200"
                        : "bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200"
                    }`}
                  >
                    {workflow.isActive ? "Active" : "Inactive"}
                  </span>
                  <span
                    className={`px-3 py-1 text-sm rounded-full ${
                      workflow.executionMode === "serial"
                        ? "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200"
                        : "bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200"
                    }`}
                  >
                    {workflow.executionMode === "serial"
                      ? "Sequential"
                      : "Parallel"}
                  </span>
                  <button className="p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300">
                    <Play className="w-4 h-4" />
                  </button>
                </div>
              </div>
            </div>

            {/* Workflow Steps */}
            {expandedWorkflow === workflow.id && (
              <div className="p-6">
                <div className="flex items-center overflow-x-auto">
                  {workflow.steps.map((step, index) =>
                    renderWorkflowStep(
                      step,
                      index === workflow.steps.length - 1,
                      workflow
                    )
                  )}
                </div>

                {/* Add Step Button */}
                <div className="mt-6 pt-4 border-t border-gray-200 dark:border-gray-700">
                  <button
                    onClick={() => setShowAddIntegration(true)}
                    className="flex items-center gap-2 px-4 py-2 text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900 rounded-lg transition-colors"
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
      <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
          Available Integrations
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {availableIntegrations.map((integration) => (
            <div
              key={integration.id}
              className="border border-gray-200 dark:border-gray-700 rounded-lg p-4 hover:border-blue-300 dark:hover:border-blue-600 transition-colors cursor-pointer"
              onClick={() => handleConfigureIntegration(integration)}
            >
              <div className="flex items-center gap-2 mb-2">
                {integration.icon}
                <span className="font-medium text-gray-900 dark:text-white">
                  {integration.name}
                </span>
                {getStatusIcon(integration.status)}
              </div>
              <p className="text-xs text-gray-600 dark:text-gray-400 mb-2">
                {integration.description}
              </p>
              <span
                className={`px-2 py-1 text-xs rounded-full ${getStatusColor(
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
        <div className="space-y-4">
          <Alert
            type="info"
            title="Feature Under Development"
            message="Integration configuration interface is currently being developed. The visual workflow designer is ready for demonstration, but the backend integration and configuration capabilities are still in progress."
          />

          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
            <h4 className="font-semibold text-gray-900 dark:text-white mb-2">
              Planned Features:
            </h4>
            <ul className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
              <li>• Drag-and-drop workflow designer</li>
              <li>• Real-time integration testing</li>
              <li>• Custom webhook configuration</li>
              <li>• Conditional branching and error handling</li>
              <li>• Integration marketplace with pre-built connectors</li>
              <li>• Monitoring and logging for each integration step</li>
            </ul>
          </div>

          <div className="flex gap-3 pt-4">
            <button
              onClick={closeModal}
              className="flex-1 px-4 py-2 bg-gray-200 dark:bg-gray-600 text-gray-800 dark:text-gray-200 rounded-md hover:bg-gray-300 dark:hover:bg-gray-500 transition-colors"
            >
              Close
            </button>
            <button
              onClick={closeModal}
              className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
            >
              Got it!
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default WorkflowManager;
