import React, { useState } from "react";
import {
  Play,
  Pause,
  CheckCircle,
  XCircle,
  Clock,
  GitCommit,
  GitPullRequest,
  Shield,
  Zap,
  Settings,
  Activity,
  Info,
} from "lucide-react";
import { Modal, Alert } from "./UI";

interface WorkflowStatus {
  id: string;
  name: string;
  description: string;
  status: "running" | "completed" | "failed" | "idle";
  lastRun?: string;
  nextRun?: string;
  icon: React.ReactNode;
  color: string;
}

const WorkflowManager: React.FC = () => {
  const [selectedWorkflow, setSelectedWorkflow] =
    useState<WorkflowStatus | null>(null);
  const [showModal, setShowModal] = useState(false);

  const workflows: WorkflowStatus[] = [
    {
      id: "commit-review",
      name: "Commit Review",
      description:
        "Automatically review commits for code quality, security vulnerabilities, and best practices",
      status: "idle",
      lastRun: "2025-01-24 14:30",
      nextRun: "On new commit",
      icon: <GitCommit className="w-6 h-6" />,
      color: "from-blue-500 to-blue-600",
    },
    {
      id: "pr-analysis",
      name: "Pull Request Analysis",
      description:
        "Comprehensive analysis of pull requests including impact assessment and merge recommendations",
      status: "completed",
      lastRun: "2025-01-24 16:45",
      nextRun: "On new PR",
      icon: <GitPullRequest className="w-6 h-6" />,
      color: "from-green-500 to-green-600",
    },
    {
      id: "security-scan",
      name: "Security Scan",
      description:
        "Deep security analysis for vulnerabilities, dependency issues, and security best practices",
      status: "running",
      lastRun: "2025-01-24 17:00",
      nextRun: "Daily at 02:00",
      icon: <Shield className="w-6 h-6" />,
      color: "from-purple-500 to-purple-600",
    },
    {
      id: "performance-check",
      name: "Performance Check",
      description:
        "Analyze code performance, identify bottlenecks, and suggest optimizations",
      status: "failed",
      lastRun: "2025-01-24 12:15",
      nextRun: "Weekly on Sunday",
      icon: <Zap className="w-6 h-6" />,
      color: "from-yellow-500 to-yellow-600",
    },
    {
      id: "code-quality",
      name: "Code Quality Gate",
      description:
        "Enforce coding standards, check for code smells, and maintain quality metrics",
      status: "idle",
      lastRun: "2025-01-23 09:30",
      nextRun: "On schedule",
      icon: <Activity className="w-6 h-6" />,
      color: "from-indigo-500 to-indigo-600",
    },
    {
      id: "documentation",
      name: "Documentation Sync",
      description:
        "Automatically update documentation based on code changes and API modifications",
      status: "completed",
      lastRun: "2025-01-24 11:20",
      nextRun: "On code changes",
      icon: <Info className="w-6 h-6" />,
      color: "from-teal-500 to-teal-600",
    },
  ];

  const getStatusIcon = (status: WorkflowStatus["status"]) => {
    switch (status) {
      case "running":
        return <Clock className="w-5 h-5 text-blue-500 animate-spin" />;
      case "completed":
        return <CheckCircle className="w-5 h-5 text-green-500" />;
      case "failed":
        return <XCircle className="w-5 h-5 text-red-500" />;
      case "idle":
        return <Pause className="w-5 h-5 text-gray-500" />;
    }
  };

  const getStatusBadge = (status: WorkflowStatus["status"]) => {
    const badges = {
      running: "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200",
      completed:
        "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200",
      failed: "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200",
      idle: "bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200",
    };

    return (
      <span
        className={`px-2 py-1 text-xs font-medium rounded-full ${badges[status]}`}
      >
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </span>
    );
  };

  const handleWorkflowAction = (workflow: WorkflowStatus) => {
    setSelectedWorkflow(workflow);
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setSelectedWorkflow(null);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            Workflow Management
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Manage and monitor automated code review workflows
          </p>
        </div>
        <button className="btn-primary flex items-center gap-2">
          <Settings className="w-4 h-4" />
          Configure Workflows
        </button>
      </div>

      {/* Status Overview */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                Running
              </p>
              <p className="text-2xl font-bold text-blue-600 dark:text-blue-400">
                {workflows.filter((w) => w.status === "running").length}
              </p>
            </div>
            <Clock className="w-8 h-8 text-blue-500" />
          </div>
        </div>
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                Completed
              </p>
              <p className="text-2xl font-bold text-green-600 dark:text-green-400">
                {workflows.filter((w) => w.status === "completed").length}
              </p>
            </div>
            <CheckCircle className="w-8 h-8 text-green-500" />
          </div>
        </div>
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                Failed
              </p>
              <p className="text-2xl font-bold text-red-600 dark:text-red-400">
                {workflows.filter((w) => w.status === "failed").length}
              </p>
            </div>
            <XCircle className="w-8 h-8 text-red-500" />
          </div>
        </div>
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600 dark:text-gray-400">
                Idle
              </p>
              <p className="text-2xl font-bold text-gray-600 dark:text-gray-400">
                {workflows.filter((w) => w.status === "idle").length}
              </p>
            </div>
            <Pause className="w-8 h-8 text-gray-500" />
          </div>
        </div>
      </div>

      {/* Workflow Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {workflows.map((workflow) => (
          <div
            key={workflow.id}
            className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm hover:shadow-md transition-shadow"
          >
            {/* Header */}
            <div
              className={`bg-gradient-to-r ${workflow.color} p-4 rounded-t-lg`}
            >
              <div className="flex items-center justify-between text-white">
                <div className="flex items-center gap-3">
                  {workflow.icon}
                  <h3 className="font-semibold">{workflow.name}</h3>
                </div>
                {getStatusIcon(workflow.status)}
              </div>
            </div>

            {/* Content */}
            <div className="p-4">
              <p className="text-gray-600 dark:text-gray-400 text-sm mb-4">
                {workflow.description}
              </p>

              {/* Status Info */}
              <div className="space-y-2 mb-4">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-gray-500 dark:text-gray-400">
                    Status:
                  </span>
                  {getStatusBadge(workflow.status)}
                </div>
                {workflow.lastRun && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-500 dark:text-gray-400">
                      Last Run:
                    </span>
                    <span className="text-sm text-gray-900 dark:text-white">
                      {workflow.lastRun}
                    </span>
                  </div>
                )}
                {workflow.nextRun && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-500 dark:text-gray-400">
                      Next Run:
                    </span>
                    <span className="text-sm text-gray-900 dark:text-white">
                      {workflow.nextRun}
                    </span>
                  </div>
                )}
              </div>

              {/* Actions */}
              <div className="flex gap-2">
                <button
                  onClick={() => handleWorkflowAction(workflow)}
                  className="flex-1 flex items-center justify-center gap-2 px-3 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                >
                  <Play className="w-4 h-4" />
                  Run
                </button>
                <button
                  onClick={() => handleWorkflowAction(workflow)}
                  className="flex items-center justify-center px-3 py-2 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                >
                  <Settings className="w-4 h-4" />
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Modal for workflow actions */}
      <Modal
        isOpen={showModal}
        onClose={closeModal}
        title={
          selectedWorkflow ? `${selectedWorkflow.name} - Under Development` : ""
        }
        maxWidth="lg"
      >
        {selectedWorkflow && (
          <div className="space-y-4">
            <Alert
              type="info"
              title="Feature Under Development"
              message="This workflow management interface is currently being developed. The visual interface is ready for demonstration, but the backend integration and workflow execution capabilities are still in progress."
            />

            <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
              <h4 className="font-semibold text-gray-900 dark:text-white mb-2">
                Planned Features:
              </h4>
              <ul className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
                <li>• Real-time workflow execution and monitoring</li>
                <li>• Custom workflow configuration and scheduling</li>
                <li>• Integration with Git webhooks for automatic triggers</li>
                <li>• Detailed execution logs and performance metrics</li>
                <li>• Advanced filtering and workflow dependencies</li>
                <li>• Email and Slack notifications for workflow events</li>
              </ul>
            </div>

            <div className="bg-blue-50 dark:bg-blue-900 rounded-lg p-4">
              <h4 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">
                Current Workflow: {selectedWorkflow.name}
              </h4>
              <p className="text-sm text-blue-800 dark:text-blue-200">
                {selectedWorkflow.description}
              </p>
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
        )}
      </Modal>
    </div>
  );
};

export default WorkflowManager;
