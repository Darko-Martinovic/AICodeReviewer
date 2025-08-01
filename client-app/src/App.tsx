import { useState, useEffect } from "react";
import { repositoryApi, commitsApi, pullRequestsApi } from "./services/api";
import type {
  Repository,
  Commit,
  PullRequest,
  CodeReview,
} from "./services/api";
import { CodeReviewResult } from "./components/CodeReviewResult";
import Header from "./components/Header";
import Navigation from "./components/Navigation";
import SearchAndActions from "./components/SearchAndActions";
import TabContent from "./components/TabContent";
import AddRepositoryModal from "./components/AddRepositoryModal";
import MainLayout from "./components/MainLayout";
import ErrorDisplay from "./components/ErrorDisplay";
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
  loading: {
    repositories: boolean;
    commits: boolean;
    pullRequests: boolean;
    review: boolean;
  };
  error: string | null;
  activeTab: TabType;
  codeReview: CodeReview | null;
  showReviewModal: boolean;
  reviewingCommits: Set<string>; // Track individual commits being reviewed
  reviewingPRs: Set<number>; // Track individual PRs being reviewed
  isInRepositoryView: boolean; // Track if we're viewing repository details
}

function App() {
  const { addToast, ToastContainer } = useToast();
  const [state, setState] = useState<AppState>({
    currentRepository: null,
    repositories: [],
    commits: [],
    pullRequests: [],
    loading: {
      repositories: false,
      commits: false,
      pullRequests: false,
      review: false,
    },
    error: null,
    activeTab: "repositories",
    codeReview: null,
    showReviewModal: false,
    reviewingCommits: new Set(),
    reviewingPRs: new Set(),
    isInRepositoryView: false,
  });

  const [searchQuery, setSearchQuery] = useState("");
  const [newRepoForm, setNewRepoForm] = useState({
    owner: "",
    name: "",
    show: false,
  });

  useEffect(() => {
    loadInitialData();
  }, []);

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
        console.log("✅ Repository found, loading commits and PRs...");
        await Promise.all([loadCommits(), loadPullRequests()]);
        console.log("📋 Data loaded for current repository");
      } else {
        console.log("❌ No current repository - ensuring clean state");
        // Ensure clean state when no repository is selected
        setState((prev) => ({
          ...prev,
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

  const loadCommits = async () => {
    try {
      console.log("📝 Loading commits...");
      setState((prev) => ({
        ...prev,
        loading: { ...prev.loading, commits: true },
      }));
      const response = await commitsApi.getRecent(20);
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
      await Promise.all([loadCommits(), loadPullRequests()]);

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
      commits: [],
      pullRequests: [],
      reviewingCommits: new Set(),
      reviewingPRs: new Set(),
    }));
  };

  const handleCommitReview = async (sha: string) => {
    try {
      console.log("🔍 Starting commit review for SHA:", sha);
      setState((prev) => ({
        ...prev,
        reviewingCommits: new Set([...prev.reviewingCommits, sha]),
      }));

      console.log("📡 Calling commits API review...");
      const response = await commitsApi.review(sha);

      console.log("✅ API Response received:", response);
      console.log("📋 Response data:", response.data);
      console.log("📋 Response data type:", typeof response.data);
      console.log("📋 Response data keys:", Object.keys(response.data || {}));

      // Log each property of the review data
      if (response.data) {
        console.log("🔍 Review data breakdown:");
        console.log("  - Summary:", response.data.summary);
        console.log("  - Summary type:", typeof response.data.summary);
        console.log("  - Summary length:", response.data.summary?.length);
        console.log("  - Issues:", response.data.issues);
        console.log("  - Issues length:", response.data.issues?.length);
        console.log("  - Suggestions:", response.data.suggestions);
        console.log(
          "  - Suggestions length:",
          response.data.suggestions?.length
        );
        console.log("  - Complexity:", response.data.complexity);
        console.log("  - Test Coverage:", response.data.testCoverage);
        console.log("  - Security:", response.data.security);
        console.log("  - Security length:", response.data.security?.length);
      } else {
        console.error("❌ Response data is null or undefined!");
      }

      setState((prev) => ({
        ...prev,
        codeReview: response.data,
        showReviewModal: true,
        reviewingCommits: new Set(
          [...prev.reviewingCommits].filter((id) => id !== sha)
        ),
      }));

      console.log("✅ State updated, modal should show");
    } catch (error) {
      console.error("❌ Commit review failed:", error);
      setState((prev) => ({
        ...prev,
        reviewingCommits: new Set(
          [...prev.reviewingCommits].filter((id) => id !== sha)
        ),
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
      setState((prev) => ({
        ...prev,
        reviewingPRs: new Set([...prev.reviewingPRs, number]),
      }));

      // Show workflow start notification
      addToast({
        type: "info",
        title: "Integration Workflow Started",
        message: "Running AI Code Review with full integration pipeline...",
      });

      console.log("📡 Calling pull requests API review...");
      const response = await pullRequestsApi.review(number);

      console.log("✅ API Response received:", response);

      // Show workflow completion notification
      addToast({
        type: "success",
        title: "Integration Workflow Complete",
        message: "AI Review → Jira Update → GitHub PR Comment completed",
        duration: 5000,
      });

      console.log("📋 Response data:", response.data);
      console.log("📋 Response data type:", typeof response.data);
      console.log("📋 Response data keys:", Object.keys(response.data || {}));

      // Log each property of the review data
      if (response.data) {
        console.log("🔍 Review data breakdown:");
        console.log("  - Summary:", response.data.summary);
        console.log("  - Summary type:", typeof response.data.summary);
        console.log("  - Summary length:", response.data.summary?.length);
        console.log("  - Issues:", response.data.issues);
        console.log("  - Issues length:", response.data.issues?.length);
        console.log("  - Suggestions:", response.data.suggestions);
        console.log(
          "  - Suggestions length:",
          response.data.suggestions?.length
        );
        console.log("  - Complexity:", response.data.complexity);
        console.log("  - Test Coverage:", response.data.testCoverage);
        console.log("  - Security:", response.data.security);
        console.log("  - Security length:", response.data.security?.length);
      } else {
        console.error("❌ Response data is null or undefined!");
      }

      setState((prev) => ({
        ...prev,
        codeReview: response.data,
        showReviewModal: true,
        reviewingPRs: new Set(
          [...prev.reviewingPRs].filter((id) => id !== number)
        ),
      }));

      console.log("✅ State updated, modal should show");
    } catch (error) {
      console.error("❌ Pull request review failed:", error);
      setState((prev) => ({
        ...prev,
        reviewingPRs: new Set(
          [...prev.reviewingPRs].filter((id) => id !== number)
        ),
      }));
      addToast({
        type: "error",
        title: "Review Failed",
        message: "Failed to review pull request",
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

  return (
    <ErrorBoundary>
      <MainLayout>
        <ToastContainer />

        {/* Header */}
        <Header
          currentRepository={state.currentRepository}
          onRefresh={loadInitialData}
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
          <TabContent
            activeTab={state.activeTab}
            currentRepository={state.currentRepository}
            repositories={filteredRepositories}
            commits={filteredCommits}
            pullRequests={filteredPullRequests}
            loading={state.loading}
            reviewingCommits={state.reviewingCommits}
            reviewingPRs={state.reviewingPRs}
            onRepositorySelect={handleRepositorySelect}
            onCommitReview={handleCommitReview}
            onPullRequestReview={handlePullRequestReview}
            onAddRepository={() =>
              setNewRepoForm((prev) => ({ ...prev, show: true }))
            }
            onTabChange={(tab) =>
              setState((prev) => ({ ...prev, activeTab: tab }))
            }
          />
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
      </MainLayout>
    </ErrorBoundary>
  );
}

export default App;
