// Types for real-time collaboration
export interface ReviewSession {
  id: string;
  commitSha: string;
  repositoryFullName: string;
  createdAt: string;
  participants: SessionParticipant[];
  comments: LiveComment[];
  status: "active" | "completed" | "archived";
  lastActivity: string;
}

export interface SessionParticipant {
  connectionId: string;
  userId: string;
  userName: string;
  avatarUrl: string;
  joinedAt: string;
  currentCursor?: CursorPosition;
  isActive: boolean;
  userColor: string;
}

export interface CursorPosition {
  fileName: string;
  lineNumber: number;
  column: number;
  lastUpdated: string;
}

export interface LiveComment {
  id: string;
  userId: string;
  userName: string;
  content: string;
  fileName: string;
  lineNumber: number;
  createdAt: string;
  updatedAt?: string;
  isResolved: boolean;
  commentType: "general" | "suggestion" | "question" | "issue";
  replies: CommentReply[];
}

export interface CommentReply {
  id: string;
  userId: string;
  userName: string;
  content: string;
  createdAt: string;
}

// WebSocket message types
export interface CursorUpdateMessage {
  userId: string;
  userName: string;
  userColor: string;
  position: CursorPosition;
}

export interface CommentMessage {
  comment: LiveComment;
  action: "create" | "update" | "delete" | "resolve";
}

export interface UserPresenceMessage {
  userId: string;
  userName: string;
  avatarUrl: string;
  userColor: string;
  action: "joined" | "left" | "typing";
  timestamp: string;
}

export interface SessionStatusMessage {
  sessionId: string;
  status: string;
  participantCount: number;
  lastActivity: string;
}

export interface FileViewMessage {
  userId: string;
  userName: string;
  fileName: string;
  timestamp: string;
}

export interface CreateSessionRequest {
  commitSha: string;
  repositoryFullName: string;
  creatorUserId: string;
  creatorUserName: string;
}
