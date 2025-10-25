import axios from "axios";

const API_BASE_URL = "https://localhost:7001/api";

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000, // Default timeout for quick operations
  headers: {
    "Content-Type": "application/json",
  },
});

// Create a separate instance for long-running operations like reviews
const longRunningApi = axios.create({
  baseURL: API_BASE_URL,
  timeout: 180000, // 3 minutes for reviews and heavy operations (was 60 seconds)
  headers: {
    "Content-Type": "application/json",
  },
});

// Response interceptor for error handling (both APIs)
api.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error("API Error:", error);
    return Promise.reject(error);
  }
);

longRunningApi.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error("API Error:", error);
    return Promise.reject(error);
  }
);

// Repository API
export const repositoryApi = {
  getCurrent: () => api.get("/repositories/current"),
  getAll: () => api.get("/repositories"),
  setRepository: (owner: string, name: string) =>
    api.post("/repositories/set", { Owner: owner, Name: name }),
};

// Commits API
export const commitsApi = {
  getBranches: () => api.get("/commits/branches"),
  getRecent: (count: number = 10, branch?: string) =>
    api.get("/commits/recent", { params: { count, branch } }),
  review: (sha: string) => {
    console.log(`🔗 API: Making request to /commits/review-workflow/${sha}`);
    return longRunningApi.post(`/commits/review-workflow/${sha}`);
  },
  getBySha: (sha: string) => api.get(`/commits/${sha}`),
  getDetails: (sha: string): Promise<{ data: { commit: CommitDetails } }> =>
    api.get(`/commits/${sha}`),
  getFileContent: (sha: string, filename: string) =>
    api.get(`/commits/${sha}/file`, { params: { filename } }),
};

// Pull Requests API
export const pullRequestsApi = {
  getAll: (state: "open" | "closed" | "all" = "open") =>
    api.get("/pullrequests", { params: { state } }),
  getById: (number: number) => api.get(`/pullrequests/${number}`),
  review: (number: number) =>
    longRunningApi.post(`/pullrequests/review/${number}`),
};

// Cache API
export const cacheApi = {
  hasCommitReview: (commitSha: string) =>
    api.get(`/cache/commit/${commitSha}/exists`),
  hasPullRequestReview: (pullRequestNumber: number) =>
    api.get(`/cache/pullrequest/${pullRequestNumber}/exists`),
  clearAll: () => api.delete("/cache/clear"),
  clearCommit: (commitSha: string) => api.delete(`/cache/commit/${commitSha}`),
  clearPullRequest: (pullRequestNumber: number) =>
    api.delete(`/cache/pullrequest/${pullRequestNumber}`),
};

// Repository Filter API
export const repositoryFilterApi = {
  getSettings: () => api.get("/repositoryfilters"),
  updateSettings: (settings: RepositoryFilterSettings) =>
    api.post("/repositoryfilters", settings),
  testPattern: (request: TestPatternRequest) =>
    api.post("/repositoryfilters/test", request),
  validatePattern: (pattern: RepositoryFilterPattern) =>
    api.post("/repositoryfilters/validate", pattern),
  getFilteredRepositories: () => api.get("/repositoryfilters/preview"),
  resetSettings: () => api.delete("/repositoryfilters"),
};

// Training API
export const trainingApi = {
  validateCode: (code: string, language: string) =>
    api.post("/training/validate-code", { code, language }),
  reviewCode: (code: string, language: string, reviewAnyway: boolean = false) =>
    longRunningApi.post("/training/review-code", {
      code,
      language,
      reviewAnyway,
    }),
  suggestPromptImprovement: (
    language: string,
    feedbackType: string,
    reviewedCode: string,
    reviewSummary: string
  ) =>
    api.post("/training/suggest-prompt-improvement", {
      language,
      feedbackType,
      reviewedCode,
      reviewSummary,
    }),
  updateCustomPrompt: (language: string, addition: string) =>
    api.post("/training/update-custom-prompt", { language, addition }),
};

// Config API
export const configApi = {
  getCodeReviewConfig: () => api.get("/config/code-review"),
};

// Types
export interface Repository {
  id: number;
  name: string;
  fullName: string;
  owner: string;
  description: string;
  defaultBranch: string;
  private: boolean;
  htmlUrl: string;
  starCount: number;
  forkCount: number;
  language: string;
  isCurrent?: boolean;
}

export interface Commit {
  sha: string;
  message: string;
  author: string;
  authorEmail: string;
  date: string;
  htmlUrl: string;
}

export interface CommitFile {
  filename: string;
  status: "added" | "modified" | "removed" | "renamed";
  additions: number;
  deletions: number;
  changes: number;
}

export interface CommitDetails {
  sha: string;
  message: string;
  author: string;
  date: string;
  url: string;
  stats: {
    additions: number;
    deletions: number;
    total: number;
  };
  files: CommitFile[];
}

export interface PullRequest {
  number: number;
  title: string;
  state: string;
  author: string;
  createdAt: string;
  updatedAt: string;
  htmlUrl: string;
  body: string;
  headBranch: string;
  baseBranch: string;
}

export interface CodeReview {
  summary: string;
  issues: CodeIssue[];
  suggestions: string[];
  complexity: "Low" | "Medium" | "High";
  testCoverage: string;
  security: SecurityIssue[];
  tokensUsed?: number;
  estimatedCost?: number;
}

export interface CodeIssue {
  severity: "Low" | "Medium" | "High" | "Critical";
  file: string;
  line: number;
  message: string;
  suggestion: string;
}

export interface SecurityIssue {
  severity: "Low" | "Medium" | "High" | "Critical";
  type: string;
  description: string;
  recommendation: string;
}

// Repository Filter Types
export interface RepositoryFilterPattern {
  pattern: string;
  provider?: string;
  owner?: string;
  caseSensitive: boolean;
  description?: string;
}

export interface RepositoryFilterSettings {
  includePatterns: RepositoryFilterPattern[];
  excludePatterns: RepositoryFilterPattern[];
  enableFiltering: boolean;
  defaultMode: FilterMode;
}

export type FilterMode = "ShowAll" | "IncludeOnly" | "ExcludeMatching";

export interface TestPatternRequest {
  repositoryName: string;
  owner?: string;
  pattern: RepositoryFilterPattern;
}

export interface CodeReviewConfig {
  maxFilesToReview: number;
  maxIssuesInSummary: number;
  showTokenMetrics: boolean;
}
