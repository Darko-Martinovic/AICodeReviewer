import React, { useState, useEffect } from "react";
import { repositoryApi, commitsApi, pullRequestsApi } from "./services/api";
import type {
  Repository,
  Commit,
  PullRequest,
  CodeReview,
} from "./services/api";
import { RepositoryCard } from "./components/RepositoryCard";
import { CommitCard } from "./components/CommitCard";
import { PullRequestCard } from "./components/PullRequestCard";
import { CodeReviewResult } from "./components/CodeReviewResult";
import {
  ErrorBoundary,
  LoadingSpinner,
  Alert,
  EmptyState,
  useToast,
} from "./components/UI";
import {
  Github,
  GitCommit,
  GitPullRequest,
  Settings,
  RefreshCw,
  Search,
  Plus,
} from "lucide-react";

type TabType = "repositories" | "commits" | "pullrequests";

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
      // Load current repository first
      const currentRepo = await getCurrentRepository();
      console.log("📁 Current repository:", currentRepo);

      if (currentRepo) {
        console.log("✅ Repository found, loading commits and PRs...");
        await loadCommits();
        await loadPullRequests();

        // Keep on repositories tab initially - user can manually switch
        console.log("📋 Staying on repositories tab");
      } else {
        console.log("❌ No current repository found");
      }
      await loadRepositories();
    } catch (error) {
      console.error("❌ Failed to load initial data:", error);
      addToast({
        type: "error",
        title: "Initialization Error",
        message:
          "Failed to load initial data. Please check your configuration.",
      });
    }
  };

  const getCurrentRepository = async () => {
    try {
      console.log("🔍 Getting current repository from backend...");
      setState((prev) => ({
        ...prev,
        loading: { ...prev.loading, repositories: true },
      }));
      const response = await repositoryApi.getCurrent();
      const repo = response.data;
      console.log("✅ Current repository from backend:", repo);

      // Find the full repository info from the repositories list
      const repositories = await repositoryApi.getAll();
      const fullRepoInfo = repositories.data.find(
        (r: Repository) => r.owner === repo.Owner && r.name === repo.Name
      );

      console.log("📋 Full repository info:", fullRepoInfo);

      setState((prev) => ({
        ...prev,
        currentRepository: fullRepoInfo || null,
        loading: { ...prev.loading, repositories: false },
      }));
      return fullRepoInfo || repo;
    } catch (error) {
      console.error("❌ No current repository set:", error);
      setState((prev) => ({
        ...prev,
        currentRepository: null,
        loading: { ...prev.loading, repositories: false },
      }));
      return null;
    }
  };

  const loadRepositories = async () => {
    try {
      console.log("📚 Loading repositories...");
      setState((prev) => ({
        ...prev,
        loading: { ...prev.loading, repositories: true },
      }));
      const response = await repositoryApi.getAll();
      console.log("📚 Repositories API response:", response.data);
      setState((prev) => ({
        ...prev,
        repositories: response.data,
        loading: { ...prev.loading, repositories: false },
        error: null,
      }));
      console.log(
        "📚 Repositories loaded successfully:",
        response.data.length,
        "repositories"
      );
    } catch (error) {
      console.error("❌ Failed to load repositories:", error);
      setState((prev) => ({
        ...prev,
        loading: { ...prev.loading, repositories: false },
        error: "Failed to load repositories",
      }));
      addToast({
        type: "error",
        title: "Error",
        message: "Failed to load repositories",
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
        // Don't auto-switch tabs - let user decide
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

      console.log("📡 Calling pull requests API review...");
      const response = await pullRequestsApi.review(number);

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
      await loadRepositories();
      await getCurrentRepository();
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
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
        <ToastContainer />

        {/* Header */}
        <header className="bg-white dark:bg-gray-800 shadow-sm border-b border-gray-200 dark:border-gray-700">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex items-center justify-between h-16">
              <div className="flex items-center gap-3">
                <Github className="w-8 h-8 text-blue-600" />
                <h1 className="text-xl font-bold text-gray-900 dark:text-white">
                  AI Code Reviewer
                </h1>
              </div>

              <div className="flex items-center gap-4">
                {state.currentRepository && (
                  <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 bg-blue-50 dark:bg-blue-900/50 border border-blue-200 dark:border-blue-800 rounded-lg">
                    <div className="w-2 h-2 bg-blue-500 rounded-full animate-pulse"></div>
                    <div className="text-sm">
                      <span className="text-gray-600 dark:text-gray-400 font-medium">
                        Current:
                      </span>{" "}
                      <span className="text-blue-700 dark:text-blue-300 font-semibold">
                        {state.currentRepository.fullName}
                      </span>
                    </div>
                  </div>
                )}
                <button
                  onClick={loadInitialData}
                  className="p-2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 transition-colors"
                  title="Refresh"
                >
                  <RefreshCw className="w-5 h-5" />
                </button>
              </div>
            </div>
          </div>
        </header>

        {/* Navigation */}
        <nav className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex space-x-8">
              <button
                onClick={() =>
                  setState((prev) => ({ ...prev, activeTab: "repositories" }))
                }
                className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
                  state.activeTab === "repositories"
                    ? "border-blue-500 text-blue-600"
                    : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                }`}
              >
                <Settings className="w-4 h-4" />
                Repositories
              </button>
              <button
                onClick={() =>
                  setState((prev) => ({ ...prev, activeTab: "commits" }))
                }
                className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
                  state.activeTab === "commits"
                    ? "border-blue-500 text-blue-600"
                    : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                }`}
              >
                <GitCommit className="w-4 h-4" />
                Commits ({state.commits.length})
              </button>
              <button
                onClick={() =>
                  setState((prev) => ({ ...prev, activeTab: "pullrequests" }))
                }
                className={`flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm ${
                  state.activeTab === "pullrequests"
                    ? "border-blue-500 text-blue-600"
                    : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                }`}
              >
                <GitPullRequest className="w-4 h-4" />
                Pull Requests ({state.pullRequests.length})
              </button>
            </div>
          </div>
        </nav>

        {/* Main Content */}
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {state.error && (
            <Alert
              type="error"
              message={state.error}
              onClose={() => setState((prev) => ({ ...prev, error: null }))}
            />
          )}

          {/* Search and Actions */}
          <div className="flex items-center justify-between mb-6">
            <div className="relative max-w-md w-full">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
              <input
                type="text"
                placeholder="Search..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="input-field pl-10"
              />
            </div>

            {state.activeTab === "repositories" && (
              <button
                onClick={() =>
                  setNewRepoForm((prev) => ({ ...prev, show: true }))
                }
                className="btn-primary flex items-center gap-2"
              >
                <Plus className="w-4 h-4" />
                Add Repository
              </button>
            )}
          </div>

          {/* Tab Content */}
          {state.activeTab === "repositories" && (
            <div>
              {state.loading.repositories ? (
                <LoadingSpinner />
              ) : filteredRepositories.length === 0 ? (
                <EmptyState
                  icon={<Settings className="w-12 h-12 text-gray-400" />}
                  title="No repositories found"
                  description="No repositories match your search criteria or none are configured."
                  action={{
                    label: "Add Repository",
                    onClick: () =>
                      setNewRepoForm((prev) => ({ ...prev, show: true })),
                  }}
                />
              ) : (
                <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                  {filteredRepositories.map((repo) => (
                    <RepositoryCard
                      key={repo.id}
                      repository={repo}
                      onSelect={handleRepositorySelect}
                      isSelected={state.currentRepository?.id === repo.id}
                    />
                  ))}
                </div>
              )}
            </div>
          )}

          {state.activeTab === "commits" && (
            <div>
              {!state.currentRepository ? (
                <EmptyState
                  icon={<GitCommit className="w-12 h-12 text-gray-400" />}
                  title="No repository selected"
                  description="Please select a repository to view commits."
                  action={{
                    label: "Select Repository",
                    onClick: () =>
                      setState((prev) => ({
                        ...prev,
                        activeTab: "repositories",
                      })),
                  }}
                />
              ) : state.loading.commits ? (
                <LoadingSpinner />
              ) : filteredCommits.length === 0 ? (
                <EmptyState
                  icon={<GitCommit className="w-12 h-12 text-gray-400" />}
                  title="No commits found"
                  description="No commits match your search criteria or the repository has no recent commits."
                />
              ) : (
                <div>
                  {/* Info note about commit limit */}
                  <div className="mb-4 p-3 bg-blue-50 border border-blue-200 rounded-md">
                    <div className="flex items-center">
                      <GitCommit className="w-4 h-4 text-blue-600 mr-2" />
                      <p className="text-sm text-blue-800">
                        <strong>Demo Mode:</strong> Showing the last 20 commits
                        only.
                        {filteredCommits.length === 20 &&
                          " This repository may have additional commits."}
                      </p>
                    </div>
                  </div>

                  <div className="space-y-4">
                    {filteredCommits.map((commit) => (
                      <CommitCard
                        key={commit.sha}
                        commit={commit}
                        onReview={handleCommitReview}
                        isReviewing={state.reviewingCommits.has(commit.sha)}
                      />
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {state.activeTab === "pullrequests" && (
            <div>
              {!state.currentRepository ? (
                <EmptyState
                  icon={<GitPullRequest className="w-12 h-12 text-gray-400" />}
                  title="No repository selected"
                  description="Please select a repository to view pull requests."
                  action={{
                    label: "Select Repository",
                    onClick: () =>
                      setState((prev) => ({
                        ...prev,
                        activeTab: "repositories",
                      })),
                  }}
                />
              ) : state.loading.pullRequests ? (
                <LoadingSpinner />
              ) : filteredPullRequests.length === 0 ? (
                <EmptyState
                  icon={<GitPullRequest className="w-12 h-12 text-gray-400" />}
                  title="No pull requests found"
                  description="No pull requests match your search criteria or the repository has no pull requests."
                />
              ) : (
                <div className="space-y-4">
                  {filteredPullRequests.map((pr) => (
                    <PullRequestCard
                      key={pr.number}
                      pullRequest={pr}
                      onReview={handlePullRequestReview}
                      isReviewing={state.reviewingPRs.has(pr.number)}
                    />
                  ))}
                </div>
              )}
            </div>
          )}
        </main>

        {/* Add Repository Modal */}
        {newRepoForm.show && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-md w-full">
              <div className="p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Add Repository
                </h3>
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Owner
                    </label>
                    <input
                      type="text"
                      value={newRepoForm.owner}
                      onChange={(e) =>
                        setNewRepoForm((prev) => ({
                          ...prev,
                          owner: e.target.value,
                        }))
                      }
                      className="input-field"
                      placeholder="e.g., microsoft"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Repository Name
                    </label>
                    <input
                      type="text"
                      value={newRepoForm.name}
                      onChange={(e) =>
                        setNewRepoForm((prev) => ({
                          ...prev,
                          name: e.target.value,
                        }))
                      }
                      className="input-field"
                      placeholder="e.g., vscode"
                    />
                  </div>
                </div>
                <div className="flex justify-end gap-3 mt-6">
                  <button
                    onClick={() =>
                      setNewRepoForm({ owner: "", name: "", show: false })
                    }
                    className="btn-secondary"
                  >
                    Cancel
                  </button>
                  <button onClick={handleAddRepository} className="btn-primary">
                    Add Repository
                  </button>
                </div>
              </div>
            </div>
          </div>
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
      </div>
    </ErrorBoundary>
  );
}

export default App;
