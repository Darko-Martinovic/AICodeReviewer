import axios from "axios";

const API_BASE_URL = "https://localhost:7001/api";

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000, // Reduced from 30s to 10s
  headers: {
    "Content-Type": "application/json",
  },
});

// Response interceptor for error handling
api.interceptors.response.use(
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
    api.post("/repositories/set", { owner, name }),
};

// Commits API
export const commitsApi = {
  getRecent: (count: number = 10) =>
    api.get("/commits/recent", { params: { count } }),
  review: (sha: string) => api.post(`/commits/review/${sha}`),
  getBySha: (sha: string) => api.get(`/commits/${sha}`),
};

// Pull Requests API
export const pullRequestsApi = {
  getAll: (state: "open" | "closed" | "all" = "open") =>
    api.get("/pullrequests", { params: { state } }),
  getById: (number: number) => api.get(`/pullrequests/${number}`),
  review: (number: number) => api.post(`/pullrequests/review/${number}`),
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
