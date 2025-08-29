import axios from "axios";
import type {
  ReviewSession,
  LiveComment,
  SessionParticipant,
  CreateSessionRequest,
} from "../types/collaboration";

const API_BASE_URL = "https://localhost:7001/api";

const collaborationApiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
  },
});

class CollaborationApiService {
  private readonly baseUrl = "/collaboration";

  async createSession(request: CreateSessionRequest): Promise<ReviewSession> {
    const response = await collaborationApiClient.post<ReviewSession>(
      `${this.baseUrl}/sessions`,
      request
    );
    return response.data;
  }

  async getSession(sessionId: string): Promise<ReviewSession> {
    const response = await collaborationApiClient.get<ReviewSession>(
      `${this.baseUrl}/sessions/${sessionId}`
    );
    return response.data;
  }

  async getActiveSessions(repository?: string): Promise<ReviewSession[]> {
    const params = repository ? { repository } : {};
    const response = await collaborationApiClient.get<ReviewSession[]>(
      `${this.baseUrl}/sessions`,
      { params }
    );
    return response.data;
  }

  async getSessionByCommit(
    commitSha: string,
    repository: string
  ): Promise<ReviewSession | null> {
    try {
      const response = await collaborationApiClient.get<ReviewSession>(
        `${this.baseUrl}/sessions/by-commit/${commitSha}`,
        { params: { repository } }
      );
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  }

  async archiveSession(sessionId: string): Promise<void> {
    await collaborationApiClient.post(
      `${this.baseUrl}/sessions/${sessionId}/archive`
    );
  }

  async getSessionComments(sessionId: string): Promise<LiveComment[]> {
    const response = await collaborationApiClient.get<LiveComment[]>(
      `${this.baseUrl}/sessions/${sessionId}/comments`
    );
    return response.data;
  }

  async getSessionParticipants(
    sessionId: string
  ): Promise<SessionParticipant[]> {
    const response = await collaborationApiClient.get<SessionParticipant[]>(
      `${this.baseUrl}/sessions/${sessionId}/participants`
    );
    return response.data;
  }

  async cleanupInactiveSessions(): Promise<void> {
    await collaborationApiClient.post(`${this.baseUrl}/cleanup`);
  }
}

export const collaborationApi = new CollaborationApiService();
