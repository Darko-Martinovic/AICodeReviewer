import { useState, useEffect } from "react";
import {
  repositoryApi,
  commitsApi,
  pullRequestsApi,
  cacheApi,
} from "./services/api";
import type {
  Repository,
  Commit,
  PullRequest,
  CodeReview,
  CommitFile,
} from "./services/api";
import { CodeReviewResult } from "./components/CodeReviewResult";
import ReviewProgressModal from "./components/ReviewProgressModal";
import Header from "./components/Header";
import Navigation from "./components/Navigation";
import SearchAndActions from "./components/SearchAndActions";
import TabContent from "./components/TabContent";
import AddRepositoryModal from "./components/AddRepositoryModal";
import MainLayout from "./components/MainLayout";
import ErrorDisplay from "./components/ErrorDisplay";
import { CollaborationDemo } from "./components/CollaborationDemo";
import { JoinSessionModal } from "./components/JoinSessionModal";
import { ErrorBoundary, useToast } from "./components/UI";

type TabType =
  | "repositories"
  | "commits"
  | "pullrequests"
  | "systemprompts"
  | "workflows";

interface AppState {
  currentRepository: Repository | null;
  repositories: Repository[];
  commits: Commit[];
  pullRequests: PullRequest[];
  branches: string[];
  selectedBranch: string;
  loading: {
    repositories: boolean;
    commits: boolean;
    pullRequests: boolean;
    branches: boolean;
    review: boolean;
  };
  error: string | null;
  activeTab: TabType;
  codeReview: CodeReview | null;
  showReviewModal: boolean;
  reviewingCommits: Set<string>; // Track individual commits being reviewed
  reviewingPRs: Set<number>; // Track individual PRs being reviewed
  isInRepositoryView: boolean; // Track if we're viewing repository details
  // Progress modal state
  progressModal: {
    show: boolean;
    type: "commit" | "pullrequest" | null;
    id: string | number | null;
    title: string;
  };
  // Collaboration state
  collaboration: {
    isActive: boolean;
    commit: Commit | null;
    files: CommitFile[];
    loading: boolean;
  };
  // Join session modal state
  showJoinSessionModal: boolean;
}

function App() {
  const { addToast, ToastContainer } = useToast();
  const [state, setState] = useState<AppState>({
    currentRepository: null,
    repositories: [],
    commits: [],
    pullRequests: [],
    branches: [],
    selectedBranch: "",
    loading: {
      repositories: false,
      commits: false,
      pullRequests: false,
      branches: false,
      review: false,
    },
    error: null,
    activeTab: "repositories",
    codeReview: null,
    showReviewModal: false,
    reviewingCommits: new Set(),
    reviewingPRs: new Set(),
    isInRepositoryView: false,
    progressModal: {
      show: false,
      type: null,
      id: null,
      title: "",
    },
    collaboration: {
      isActive: false,
      commit: null,
      files: [],
      loading: false,
    },
    showJoinSessionModal: false,
  });

  const [searchQuery, setSearchQuery] = useState("");
  const [newRepoForm, setNewRepoForm] = useState({
    owner: "",
    name: "",
    show: false,
  });

  useEffect(() => {
    loadInitialData();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const handleCommitCollaboration = async (commit: Commit) => {
    // Start loading state
    setState((prev) => ({
      ...prev,
      collaboration: {
        isActive: true,
        commit: commit,
        files: [],
        loading: true,
      },
    }));

    addToast({
      type: "info",
      title: "Loading Collaboration",
      message: "Fetching commit files...",
    });

    try {
      // Fetch the commit details with files
      const response = await commitsApi.getDetails(commit.sha);
      const commitDetails = response.data.commit;

      // Update state with the fetched files
      setState((prev) => ({
        ...prev,
        collaboration: {
          isActive: true,
          commit: commit,
          files: commitDetails.files,
          loading: false,
        },
      }));

      addToast({
        type: "success",
        title: "Collaboration Started",
        message: `Ready to collaborate on ${commitDetails.files.length} file(s)`,
      });
    } catch (error) {
      console.error("Failed to fetch commit files:", error);
      setState((prev) => ({
        ...prev,
        collaboration: {
          isActive: false,
          commit: null,
          files: [],
          loading: false,
        },
      }));

      addToast({
        type: "error",
        title: "Collaboration Failed",
        message: "Failed to load commit files. Please try again.",
      });
    }
  };

  const handleEndCollaboration = () => {
    setState((prev) => ({
      ...prev,
      collaboration: {
        isActive: false,
        commit: null,
        files: [],
        loading: false,
      },
    }));
    addToast({
      type: "info",
      title: "Collaboration Ended",
      message: "Collaborative review session ended",
    });
  };

  const handleOpenJoinSessionModal = () => {
    setState((prev) => ({
      ...prev,
      showJoinSessionModal: true,
    }));
  };

  const handleCloseJoinSessionModal = () => {
    setState((prev) => ({
      ...prev,
      showJoinSessionModal: false,
    }));
  };

  const handleJoinSession = async (sessionId: string, username: string) => {
    try {
      addToast({
        type: "info",
        title: "Joining Session",
        message: `Joining collaboration session ${sessionId}...`,
      });

      // Here you would typically call the collaboration API to join the session
      // For now, we'll just simulate success

      addToast({
        type: "success",
        title: "Session Joined",
        message: `Successfully joined collaboration session with username "${username}"`,
      });

      handleCloseJoinSessionModal();
    } catch (error) {
      console.error("Failed to join session:", error);
      addToast({
        type: "error",
        title: "Join Failed",
        message:
          "Failed to join collaboration session. Please check the session ID and try again.",
      });
    }
  };

  const loadInitialData = async () => {
    try {
      console.log("🚀 Loading initial data...");

      // Load repositories and current repository info together
      const [currentRepoResponse, repositoriesResponse] = await Promise.all([
        repositoryApi.getCurrent().catch(() => null), // Don't fail if no current repo
        repositoryApi.getAll(),
      ]);

      console.log(
        "📚 Repositories loaded:",
        repositoriesResponse.data.length,
        "repositories"
      );

      // Update repositories in state
      setState((prev) => ({
        ...prev,
        repositories: repositoriesResponse.data,
        loading: { ...prev.loading, repositories: false },
      }));

      // Find current repository if it exists
      let currentRepo = null;
      if (currentRepoResponse?.data) {
        const repo = currentRepoResponse.data;
        console.log("✅ Current repository from backend:", repo);

        currentRepo = repositoriesResponse.data.find(
          (r: Repository) => r.owner === repo.Owner && r.name === repo.Name
        );
        console.log("📁 Current repository resolved:", currentRepo);
      } else {
        console.log("❌ No current repository found");
      }

      // Update current repository in state
      setState((prev) => ({
        ...prev,
        currentRepository: currentRepo,
      }));

      if (currentRepo) {
        console.log(
          "✅ Repository found, loading branches, commits and PRs..."
        );
        await Promise.all([loadBranches(), loadCommits(), loadPullRequests()]);
        console.log("📋 Data loaded for current repository");
      } else {
        console.log("❌ No current repository - ensuring clean state");
        // Ensure clean state when no repository is selected
        setState((prev) => ({
          ...prev,
          branches: [],
          selectedBranch: "",
          commits: [],
          pullRequests: [],
        }));
      }
    } catch (error) {
      console.error("❌ Failed to load initial data:", error);
      setState((prev) => ({
        ...prev,
        loading: { ...prev.loading, repositories: false },
      }));
      addToast({
        type: "error",
        title: "Initialization Error",
        message:
          "Failed to load initial data. Please check your configuration.",
      });
    }
  };

  const loadBranches = async () => {
    try {
      console.log("🌿 Loading branches...");
      setState((prev) => ({
        ...prev,
        loading: { ...prev.loading, branches: true },
      }));
      const response = await commitsApi.getBranches();
      console.log("🌿 Branches API response:", response.data);

      const branches = response.data.branches || [];
      setState((prev) => ({
        ...prev,
        branches,
        selectedBranch: prev.selectedBranch || branches[0] || "",
        loading: { ...prev.loading, branches: false },
      }));
      console.log(
        "🌿 Branches loaded successfully:",
        branches.length,
        "branches"
      );
    } catch (error) {
      console.error("❌ Failed to load branches:", error);
      setState((prev) => ({
        ...prev,
        branches: [],
        loading: { ...prev.loading, branches: false },
      }));
    }
  };

  const loadCommits = async (branch?: string) => {
    try {
      console.log("📝 Loading commits for branch:", branch || "default");
      setState((prev) => ({
        ...prev,
        loading: { ...prev.loading, commits: true },
      }));
      const response = await commitsApi.getRecent(20, branch);
      console.log("📝 Commits API response:", response.data);
      console.log("📝 Commits array:", response.data.commits);

      setState((prev) => ({
        ...prev,
        commits: response.data.commits || [],
        loading: { ...prev.loading, commits: false },
        error: null,
      }));
      console.log(
        "📝 Commits loaded successfully:",
        response.data.commits?.length || 0,
        "commits"
      );
      console.log(
        "📝 State updated. New commits array length:",
        response.data.commits?.length || 0
      );
    } catch (error) {
      console.error("❌ Failed to load commits:", error);

      // Handle different error types more gracefully
      let errorMessage = "Failed to load commits";
      if (error && typeof error === "object" && "response" in error) {
        const axiosError = error as { response?: { status: number } };
        if (axiosError.response?.status === 400) {
          errorMessage =
            "Repository has no accessible commits or default branch not found";
        } else if (axiosError.response?.status === 404) {
          errorMessage = "Repository not found or not accessible";
        } else if (axiosError.response?.status === 403) {
          errorMessage = "Access denied to repository commits";
        }
      }

      setState((prev) => ({
        ...prev,
        commits: [], // Clear commits on error
        loading: { ...prev.loading, commits: false },
        error: null, // Don't show global error for this
      }));

      addToast({
        type: "warning",
        title: "Commits Unavailable",
        message: errorMessage,
      });
    }
  };

  const loadPullRequests = async () => {
    try {
      console.log("📋 Loading pull requests...");
      setState((prev) => ({
        ...prev,
        loading: { ...prev.loading, pullRequests: true },
      }));
      const response = await pullRequestsApi.getAll("all");
      console.log("📋 Pull requests API response:", response.data);
      setState((prev) => ({
        ...prev,
        pullRequests: response.data,
        loading: { ...prev.loading, pullRequests: false },
        error: null,
      }));
      console.log(
        "📋 Pull requests loaded successfully:",
        response.data.length,
        "pull requests"
      );
    } catch (error) {
      console.error("❌ Failed to load pull requests:", error);

      // Handle different error types more gracefully
      let errorMessage = "Failed to load pull requests";
      if (error && typeof error === "object" && "response" in error) {
        const axiosError = error as { response?: { status: number } };
        if (axiosError.response?.status === 400) {
          errorMessage =
            "Repository has no accessible pull requests or not found";
        } else if (axiosError.response?.status === 404) {
          errorMessage = "Repository not found or not accessible";
        } else if (axiosError.response?.status === 403) {
          errorMessage = "Access denied to repository pull requests";
        }
      }

      setState((prev) => ({
        ...prev,
        pullRequests: [], // Clear pull requests on error
        loading: { ...prev.loading, pullRequests: false },
        error: null, // Don't show global error for this
      }));

      addToast({
        type: "warning",
        title: "Pull Requests Unavailable",
        message: errorMessage,
      });
    }
  };

  const handleRepositorySelect = async (repository: Repository) => {
    try {
      console.log("🔄 Switching to repository:", repository.fullName);

      // First update the backend
      await repositoryApi.setRepository(repository.owner, repository.name);

      // Update the frontend state
      setState((prev) => ({
        ...prev,
        currentRepository: repository,
        isInRepositoryView: true,
        activeTab: "commits", // Switch to commits tab when entering repository
        // Clear existing data while loading new data
        commits: [],
        pullRequests: [],
        reviewingCommits: new Set(),
        reviewingPRs: new Set(),
        loading: {
          ...prev.loading,
          commits: true,
          pullRequests: true,
        },
      }));

      console.log("✅ Repository switched, loading new data...");

      addToast({
        type: "success",
        title: "Repository Selected",
        message: `Switched to ${repository.fullName}`,
      });

      // Load new data for the selected repository
      await Promise.all([loadBranches(), loadCommits(), loadPullRequests()]);

      console.log("✅ Repository data loaded successfully");
    } catch (error) {
      console.error("❌ Failed to switch repository:", error);
      addToast({
        type: "error",
        title: "Error",
        message: "Failed to select repository",
      });
    }
  };

  const handleExitRepository = () => {
    setState((prev) => ({
      ...prev,
      isInRepositoryView: false,
      activeTab: "repositories",
      // Keep currentRepository but clear view-specific data
      branches: [],
      selectedBranch: "",
      commits: [],
      pullRequests: [],
      reviewingCommits: new Set(),
      reviewingPRs: new Set(),
    }));
  };

  const handleBranchSelect = async (branch: string) => {
    console.log("🌿 Switching to branch:", branch);
    setState((prev) => ({
      ...prev,
      selectedBranch: branch,
    }));

    // Reload commits for the selected branch
    await loadCommits(branch);
  };

  const handleCommitReview = async (sha: string) => {
    try {
      console.log("🔍 Starting commit review for SHA:", sha);

      // Find the commit for display
      const commit = state.commits.find((c) => c.sha === sha);
      const commitTitle = commit
        ? commit.message.split("\n")[0]
        : `Commit ${sha.substring(0, 8)}`;

      // Check cache status first
      console.log("📋 Checking cache for commit:", sha);
      const cacheResponse = await cacheApi.hasCommitReview(sha);
      const isCached = cacheResponse.data;

      console.log(
        "📋 Cache status for commit",
        sha,
        ":",
        isCached ? "CACHED" : "NOT CACHED"
      );

      if (isCached) {
        addToast({
          type: "info",
          title: "Using Cached Results",
          message: `Review for commit ${sha.substring(
            0,
            8
          )} will load instantly from cache.`,
        });
      } else {
        addToast({
          type: "info",
          title: "Starting Fresh Review",
          message: `Analyzing commit ${sha.substring(
            0,
            8
          )} - expected duration: ~23 seconds.`,
        });
      }

      // Show progress modal and start tracking
      setState((prev) => ({
        ...prev,
        reviewingCommits: new Set([...prev.reviewingCommits, sha]),
        progressModal: {
          show: true,
          type: "commit",
          id: sha,
          title: `${
            isCached ? "Loading Cached Review" : "Reviewing"
          }: ${commitTitle}`,
        },
      }));

      console.log("📡 Calling commits API review...");
      const startTime = Date.now();
      const response = await commitsApi.review(sha);
      const duration = Date.now() - startTime;

      console.log(`✅ API Response received in ${duration}ms:`, response);
      console.log("📋 Response data:", response.data);

      // Log each property of the review data
      if (response.data) {
        console.log("🔍 Review data breakdown:");
        console.log("  - Summary:", response.data.summary);
        console.log("  - Issues length:", response.data.issues?.length);
        console.log(
          "  - Suggestions length:",
          response.data.suggestions?.length
        );
        console.log("  - Complexity:", response.data.complexity);
        console.log("  - Test Coverage:", response.data.testCoverage);
        console.log("  - Security length:", response.data.security?.length);
      }

      // Wait a bit to let the progress modal complete its animation (less time if cached)
      const delayMs = isCached ? 1000 : 2000;
      setTimeout(() => {
        setState((prev) => ({
          ...prev,
          codeReview: response.data,
          showReviewModal: true,
          reviewingCommits: new Set(
            [...prev.reviewingCommits].filter((id) => id !== sha)
          ),
          progressModal: {
            ...prev.progressModal,
            show: false,
          },
        }));
      }, delayMs);

      console.log("✅ State updated, modal should show");
    } catch (error) {
      console.error("❌ Commit review failed:", error);
      setState((prev) => ({
        ...prev,
        reviewingCommits: new Set(
          [...prev.reviewingCommits].filter((id) => id !== sha)
        ),
        progressModal: {
          ...prev.progressModal,
          show: false,
        },
      }));
      addToast({
        type: "error",
        title: "Review Failed",
        message: "Failed to review commit",
      });
    }
  };

  const handlePullRequestReview = async (number: number) => {
    try {
      console.log("🔍 Starting pull request review for number:", number);

      // Find the PR for display
      const pr = state.pullRequests.find((p) => p.number === number);
      const prTitle = pr ? pr.title : `Pull Request #${number}`;

      // Check cache status first
      console.log("📋 Checking cache for PR:", number);
      const cacheResponse = await cacheApi.hasPullRequestReview(number);
      const isCached = cacheResponse.data;

      console.log(
        "📋 Cache status for PR",
        number,
        ":",
        isCached ? "CACHED" : "NOT CACHED"
      );

      if (isCached) {
        addToast({
          type: "info",
          title: "Using Cached Results",
          message: `Review for PR #${number} will load instantly from cache.`,
        });
      } else {
        addToast({
          type: "info",
          title: "Starting Fresh Review",
          message: `Analyzing PR #${number} - expected duration: ~3 minutes.`,
        });
      }

      // Show progress modal and start tracking
      setState((prev) => ({
        ...prev,
        reviewingPRs: new Set([...prev.reviewingPRs, number]),
        progressModal: {
          show: true,
          type: "pullrequest",
          id: number,
          title: `${
            isCached ? "Loading Cached Review" : "Reviewing"
          }: ${prTitle}`,
        },
      }));

      console.log("📡 Calling pull requests API review...");
      console.log("⏰ API call started at:", new Date().toISOString());
      const startTime = Date.now();
      const response = await pullRequestsApi.review(number);
      const duration = Date.now() - startTime;

      console.log(`✅ API Response received in ${duration}ms:`, response);
      console.log("⏰ API call completed at:", new Date().toISOString());

      console.log("📋 Response data:", response.data);

      // Log each property of the review data
      if (response.data) {
        console.log("🔍 Review data breakdown:");
        console.log("  - Summary:", response.data.summary);
        console.log("  - Issues length:", response.data.issues?.length);
        console.log(
          "  - Suggestions length:",
          response.data.suggestions?.length
        );
        console.log("  - Complexity:", response.data.complexity);
        console.log("  - Test Coverage:", response.data.testCoverage);
        console.log("  - Security length:", response.data.security?.length);
      }

      // Wait a bit to let the progress modal complete its animation (less time if cached)
      const delayMs = isCached ? 1000 : 2000;
      setTimeout(() => {
        console.log(
          "🔄 About to set PR review state with response:",
          response.data
        );

        setState((prev) => ({
          ...prev,
          codeReview: response.data,
          showReviewModal: true,
          reviewingPRs: new Set(
            [...prev.reviewingPRs].filter((id) => id !== number)
          ),
          progressModal: {
            ...prev.progressModal,
            show: false,
          },
        }));

        console.log("✅ PR review state updated - modal should show now");

        // Show workflow completion notification
        addToast({
          type: "success",
          title: isCached
            ? "Cached Review Loaded"
            : "Integration Workflow Complete",
          message: isCached
            ? `Cached review for PR #${number} loaded successfully`
            : "AI Review → JIRA Update → GitHub PR Comment completed",
          duration: 5000,
        });
      }, delayMs);

      console.log("✅ State updated, modal should show");
    } catch (error) {
      console.error("❌ Pull request review failed:", error);

      // Check if it's a timeout error
      const errorObj = error as { code?: string; message?: string };
      const isTimeout =
        errorObj?.code === "ECONNABORTED" ||
        errorObj?.message?.includes("timeout");
      const errorMessage = isTimeout
        ? "Review timed out - PR reviews take longer due to GitHub/JIRA integration. Please try again."
        : "Failed to review pull request";

      setState((prev) => ({
        ...prev,
        reviewingPRs: new Set(
          [...prev.reviewingPRs].filter((id) => id !== number)
        ),
        progressModal: {
          ...prev.progressModal,
          show: false,
        },
      }));
      addToast({
        type: "error",
        title: isTimeout ? "Review Timeout" : "Review Failed",
        message: errorMessage,
        duration: isTimeout ? 8000 : 5000, // Show timeout message longer
      });
    }
  };

  const handleAddRepository = async () => {
    if (!newRepoForm.owner || !newRepoForm.name) {
      addToast({
        type: "warning",
        title: "Invalid Input",
        message: "Please provide both owner and repository name",
      });
      return;
    }

    try {
      await repositoryApi.setRepository(newRepoForm.owner, newRepoForm.name);
      setNewRepoForm({ owner: "", name: "", show: false });
      addToast({
        type: "success",
        title: "Repository Added",
        message: `Successfully added ${newRepoForm.owner}/${newRepoForm.name}`,
      });
      // Reload initial data to get updated repositories and current repository
      await loadInitialData();
    } catch {
      addToast({
        type: "error",
        title: "Error",
        message: "Failed to add repository",
      });
    }
  };

  const filteredRepositories = state.repositories
    .filter(
      (repo) =>
        repo.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        repo.fullName.toLowerCase().includes(searchQuery.toLowerCase())
    )
    .sort((a, b) => {
      // Current repository always first
      if (a.isCurrent) return -1;
      if (b.isCurrent) return 1;
      // Then sort by ID (which represents the order from backend - most recently used first)
      return a.id - b.id;
    });

  const filteredCommits = state.commits.filter(
    (commit) =>
      commit.message.toLowerCase().includes(searchQuery.toLowerCase()) ||
      commit.author.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const filteredPullRequests = state.pullRequests.filter(
    (pr) =>
      pr.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      pr.author.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // Helper function to detect language from filename
  const getLanguageFromFilename = (filename: string): string => {
    const extension = filename.split(".").pop()?.toLowerCase();
    const languageMap: Record<string, string> = {
      ts: "TypeScript",
      tsx: "TypeScript",
      js: "JavaScript",
      jsx: "JavaScript",
      py: "Python",
      java: "Java",
      cs: "CSharp",
      cpp: "C++",
      c: "C",
      go: "Go",
      rs: "Rust",
      php: "PHP",
      rb: "Ruby",
      sql: "SQL",
      json: "JSON",
      xml: "XML",
      html: "HTML",
      css: "CSS",
      md: "Markdown",
      sh: "Shell",
    };
    return languageMap[extension || ""] || "Text";
  };

  // Helper function to generate simple content preview based on file info
  const generateFilePreview = (file: CommitFile): string => {
    const extension = file.filename.split(".").pop()?.toLowerCase();
    const statusEmoji =
      file.status === "added"
        ? "🆕"
        : file.status === "modified"
        ? "✏️"
        : file.status === "removed"
        ? "🗑️"
        : "📝";

    return `// ${statusEmoji} ${file.filename} (${file.status})
// Changes: +${file.additions} -${file.deletions}
// Total lines affected: ${file.changes}

// This is a preview of ${file.filename}
// File extension: ${extension}
// Language: ${getLanguageFromFilename(file.filename)}

${
  extension === "ts" || extension === "tsx"
    ? `// TypeScript/React file
interface Example {
  id: string;
  name: string;
}

export const process = (data: Example) => {
  console.log('Processing:', data.name);
  return data;
};`
    : extension === "js" || extension === "jsx"
    ? `// JavaScript/React file
function processData(data) {
  console.log('Processing:', data.name);
  return data;
}

module.exports = { processData };`
    : extension === "py"
    ? `# Python file
def process_data(data):
    print(f"Processing: {data['name']}")
    return data

if __name__ == "__main__":
    process_data({"name": "example"})`
    : `// ${getLanguageFromFilename(file.filename)} file
// File: ${file.filename}
// Status: ${file.status}
console.log("File loaded successfully");`
}

// Changes in this commit:
// +${file.additions} lines added
// -${file.deletions} lines removed`;
  };

  return (
    <ErrorBoundary>
      <MainLayout>
        <ToastContainer />

        {/* Header */}
        <Header
          currentRepository={state.currentRepository}
          onRefresh={loadInitialData}
          onJoinSession={handleOpenJoinSessionModal}
        />

        {/* Navigation */}
        <Navigation
          activeTab={state.activeTab}
          onTabChange={(tab) =>
            setState((prev) => ({ ...prev, activeTab: tab }))
          }
          currentRepository={state.currentRepository}
          commits={state.commits}
          pullRequests={state.pullRequests}
          isInRepositoryView={state.isInRepositoryView}
          onExitRepository={handleExitRepository}
        />

        {/* Main Content */}
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {state.error && (
            <ErrorDisplay
              error={state.error}
              onClose={() => setState((prev) => ({ ...prev, error: null }))}
            />
          )}

          {/* Search and Actions */}
          <SearchAndActions
            searchQuery={searchQuery}
            onSearchChange={setSearchQuery}
            activeTab={state.activeTab}
            onAddRepository={() =>
              setNewRepoForm((prev) => ({ ...prev, show: true }))
            }
          />

          {/* Tab Content */}
          {state.collaboration.isActive && state.collaboration.commit ? (
            <div>
              <div
                style={{ padding: "16px", borderBottom: "1px solid #e5e7eb" }}
              >
                <button
                  onClick={handleEndCollaboration}
                  style={{
                    background: "#6b7280",
                    color: "white",
                    border: "none",
                    borderRadius: "6px",
                    padding: "8px 16px",
                    cursor: "pointer",
                    marginBottom: "8px",
                  }}
                >
                  ← Back to Repository
                </button>
              </div>
              {state.collaboration.loading ? (
                <div style={{ padding: "40px", textAlign: "center" }}>
                  <div style={{ marginBottom: "16px" }}>
                    Loading commit files...
                  </div>
                  <div className="spinner"></div>
                </div>
              ) : (
                <CollaborationDemo
                  commitSha={state.collaboration.commit.sha}
                  repositoryFullName={state.currentRepository?.fullName || ""}
                  files={state.collaboration.files.map((file) => ({
                    filename: file.filename,
                    content: generateFilePreview(file),
                    language: getLanguageFromFilename(file.filename),
                    status:
                      file.status === "removed"
                        ? "deleted"
                        : file.status === "renamed"
                        ? "modified"
                        : file.status,
                  }))}
                  currentUser={{
                    id: "demo-user",
                    name: "Demo User",
                    avatarUrl: undefined,
                  }}
                />
              )}
            </div>
          ) : (
            <TabContent
              activeTab={state.activeTab}
              currentRepository={state.currentRepository}
              repositories={filteredRepositories}
              commits={filteredCommits}
              pullRequests={filteredPullRequests}
              branches={state.branches}
              selectedBranch={state.selectedBranch}
              loading={state.loading}
              reviewingCommits={state.reviewingCommits}
              reviewingPRs={state.reviewingPRs}
              onRepositorySelect={handleRepositorySelect}
              onCommitReview={handleCommitReview}
              onCommitCollaborate={handleCommitCollaboration}
              onPullRequestReview={handlePullRequestReview}
              onBranchSelect={handleBranchSelect}
              onAddRepository={() =>
                setNewRepoForm((prev) => ({ ...prev, show: true }))
              }
              onTabChange={(tab) =>
                setState((prev) => ({ ...prev, activeTab: tab }))
              }
            />
          )}
        </main>

        {/* Add Repository Modal */}
        <AddRepositoryModal
          newRepoForm={newRepoForm}
          onFormChange={(updates) =>
            setNewRepoForm((prev) => ({ ...prev, ...updates }))
          }
          onAddRepository={handleAddRepository}
          onClose={() => setNewRepoForm({ owner: "", name: "", show: false })}
        />

        {/* Join Session Modal */}
        {state.showJoinSessionModal && (
          <JoinSessionModal
            isOpen={state.showJoinSessionModal}
            currentSessionId={
              state.collaboration.isActive ? "current-session-id" : undefined
            }
            onClose={handleCloseJoinSessionModal}
            onJoinSession={handleJoinSession}
          />
        )}

        {/* Code Review Modal */}
        {state.showReviewModal && state.codeReview && (
          <CodeReviewResult
            review={state.codeReview}
            onClose={() =>
              setState((prev) => ({
                ...prev,
                showReviewModal: false,
                codeReview: null,
              }))
            }
          />
        )}

        {/* Progress Modal */}
        {state.progressModal.show &&
          state.progressModal.type &&
          state.progressModal.id && (
            <ReviewProgressModal
              isOpen={state.progressModal.show}
              onClose={() => {
                console.log("🚪 Progress modal manually closed by user");
                console.log(
                  "🚪 Current state - codeReview:",
                  state.codeReview ? "EXISTS" : "NULL"
                );
                console.log(
                  "🚪 Current state - showReviewModal:",
                  state.showReviewModal
                );

                setState((prev) => ({
                  ...prev,
                  progressModal: { ...prev.progressModal, show: false },
                  // Show review modal if there's a review available
                  showReviewModal: prev.codeReview
                    ? true
                    : prev.showReviewModal,
                  reviewingCommits:
                    prev.progressModal.type === "commit" &&
                    prev.progressModal.id
                      ? new Set(
                          [...prev.reviewingCommits].filter(
                            (id) => id !== prev.progressModal.id
                          )
                        )
                      : prev.reviewingCommits,
                  reviewingPRs:
                    prev.progressModal.type === "pullrequest" &&
                    prev.progressModal.id
                      ? new Set(
                          [...prev.reviewingPRs].filter(
                            (id) => id !== prev.progressModal.id
                          )
                        )
                      : prev.reviewingPRs,
                }));

                console.log(
                  "🚪 After manual close - review modal should show if data exists"
                );

                // Start polling for review results in case API completes later
                console.log("⏰ Starting polling for review results...");
                const pollInterval = setInterval(() => {
                  setState((prev) => {
                    if (prev.codeReview && !prev.showReviewModal) {
                      console.log(
                        "🎯 Found review results - showing modal now!"
                      );
                      clearInterval(pollInterval);
                      return {
                        ...prev,
                        showReviewModal: true,
                      };
                    }
                    return prev;
                  });
                }, 2000);

                // For PR reviews, poll for longer since they take more time
                const reviewType = state.progressModal.type;
                const pollDuration =
                  reviewType === "pullrequest" ? 300000 : 60000; // 5 minutes for PRs, 1 minute for commits
                setTimeout(() => {
                  clearInterval(pollInterval);
                  console.log(
                    `⏰ Stopped polling for review results after ${
                      pollDuration / 1000
                    } seconds`
                  );
                }, pollDuration);
              }}
              reviewType={state.progressModal.type}
              reviewId={state.progressModal.id}
              title={state.progressModal.title}
              forceCompleted={false}
            />
          )}
      </MainLayout>
    </ErrorBoundary>
  );
}

export default App;
