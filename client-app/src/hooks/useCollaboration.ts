import { useState, useEffect, useCallback, useRef } from "react";
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
} from "@microsoft/signalr";
import type {
  SessionParticipant,
  CursorPosition,
  LiveComment,
  CommentReply,
  CursorUpdateMessage,
  CommentMessage,
  UserPresenceMessage,
  FileViewMessage,
} from "../types/collaboration";

interface UseCollaborationProps {
  sessionId: string;
  currentUser: {
    id: string;
    name: string;
    avatarUrl?: string;
  };
  onCurrentFileReceived?: (fileInfo: {
    fileName: string;
    fileContent: string;
    fileLanguage: string;
  }) => void;
}

interface CollaborationState {
  participants: SessionParticipant[];
  cursors: CursorUpdateMessage[];
  comments: LiveComment[];
  isConnected: boolean;
  connectionState: HubConnectionState;
  error: string | null;
  typingUsers: Array<{ userId: string; fileName: string }>;
}

export const useCollaboration = ({
  sessionId,
  currentUser,
  onCurrentFileReceived,
}: UseCollaborationProps) => {
  const [state, setState] = useState<CollaborationState>({
    participants: [],
    cursors: [],
    comments: [],
    isConnected: false,
    connectionState: HubConnectionState.Disconnected,
    error: null,
    typingUsers: [],
  });

  const connectionRef = useRef<HubConnection | null>(null);
  const reconnectTimeoutRef = useRef<NodeJS.Timeout | undefined>(undefined);

  // Initialize connection and join session
  useEffect(() => {
    let isActive = true;

    const initializeAndJoin = async () => {
      try {
        if (connectionRef.current) {
          await connectionRef.current.stop();
        }

        const connection = new HubConnectionBuilder()
          .withUrl(`https://localhost:7001/collaborationHub`, {
            withCredentials: false,
          })
          .withAutomaticReconnect([0, 2000, 10000, 30000])
          .build();

        // Connection state handlers
        connection.onreconnecting(() => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              connectionState: HubConnectionState.Reconnecting,
              isConnected: false,
            }));
          }
        });

        connection.onreconnected(async () => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              connectionState: HubConnectionState.Connected,
              isConnected: true,
              error: null,
            }));
            try {
              console.log("🔵 Reconnected - Calling JoinSession:", {
                sessionId,
                userId: currentUser.id,
                userName: currentUser.name,
                avatarUrl: currentUser.avatarUrl || "",
              });

              await connection.invoke(
                "JoinSession",
                sessionId,
                currentUser.id,
                currentUser.name,
                currentUser.avatarUrl || ""
              );

              console.log("🔵 Reconnection JoinSession completed successfully");
            } catch (error) {
              console.error("Failed to rejoin session:", error);
            }
          }
        });

        connection.onclose((error) => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              connectionState: HubConnectionState.Disconnected,
              isConnected: false,
              error: error?.message || "Connection closed",
            }));

            if (reconnectTimeoutRef.current) {
              clearTimeout(reconnectTimeoutRef.current);
            }
            reconnectTimeoutRef.current = setTimeout(() => {
              if (isActive) {
                initializeAndJoin();
              }
            }, 5000);
          }
        });

        // Event handlers
        connection.on(
          "SessionState",
          (sessionState: {
            participants: SessionParticipant[];
            comments: LiveComment[];
            cursors: CursorUpdateMessage[];
            currentFileName?: string;
            currentFileContent?: string;
            currentFileLanguage?: string;
          }) => {
            console.log("🔵 SessionState received:", {
              participantsCount: sessionState.participants?.length || 0,
              participants: sessionState.participants,
              commentsCount: sessionState.comments?.length || 0,
              cursorsCount: sessionState.cursors?.length || 0,
              currentFileName: sessionState.currentFileName,
              hasFileContent: !!sessionState.currentFileContent,
            });

            if (isActive) {
              setState((prev) => ({
                ...prev,
                participants: sessionState.participants || [],
                comments: sessionState.comments || [],
                cursors: sessionState.cursors || [],
              }));

              // If there's a current file in the session, trigger callback
              if (
                sessionState.currentFileName &&
                sessionState.currentFileContent
              ) {
                // We'll handle this with a callback
                if (onCurrentFileReceived) {
                  onCurrentFileReceived({
                    fileName: sessionState.currentFileName,
                    fileContent: sessionState.currentFileContent,
                    fileLanguage: sessionState.currentFileLanguage || "text",
                  });
                }
              }
            }
          }
        );

        connection.on("UserJoined", (userPresence: UserPresenceMessage) => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              participants: [
                ...prev.participants.filter(
                  (p) => p.userId !== userPresence.userId
                ),
                {
                  connectionId: "",
                  userId: userPresence.userId,
                  userName: userPresence.userName,
                  avatarUrl: userPresence.avatarUrl,
                  joinedAt: userPresence.timestamp,
                  isActive: true,
                  userColor: userPresence.userColor,
                },
              ],
            }));
          }
        });

        connection.on("UserLeft", (connectionId: string) => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              participants: prev.participants.filter(
                (p) => p.connectionId !== connectionId
              ),
              cursors: prev.cursors.filter(
                (c) =>
                  c.userId !==
                  prev.participants.find((p) => p.connectionId === connectionId)
                    ?.userId
              ),
            }));
          }
        });

        connection.on("CursorMoved", (cursorMessage: CursorUpdateMessage) => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              cursors: [
                ...prev.cursors.filter(
                  (c) => c.userId !== cursorMessage.userId
                ),
                cursorMessage,
              ],
            }));
          }
        });

        connection.on("CommentAdded", (commentMessage: CommentMessage) => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              comments: [...prev.comments, commentMessage.comment],
            }));
          }
        });

        connection.on("CommentUpdated", (commentMessage: CommentMessage) => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              comments: prev.comments.map((c) =>
                c.id === commentMessage.comment.id ? commentMessage.comment : c
              ),
            }));
          }
        });

        connection.on("CommentDeleted", (commentId: string) => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              comments: prev.comments.filter((c) => c.id !== commentId),
            }));
          }
        });

        connection.on(
          "CommentResolved",
          (data: { commentId: string; isResolved: boolean }) => {
            if (isActive) {
              setState((prev) => ({
                ...prev,
                comments: prev.comments.map((c) =>
                  c.id === data.commentId
                    ? { ...c, isResolved: data.isResolved }
                    : c
                ),
              }));
            }
          }
        );

        connection.on(
          "CommentReplyAdded",
          (data: { commentId: string; reply: CommentReply }) => {
            if (isActive) {
              setState((prev) => ({
                ...prev,
                comments: prev.comments.map((c) =>
                  c.id === data.commentId
                    ? { ...c, replies: [...c.replies, data.reply] }
                    : c
                ),
              }));
            }
          }
        );

        connection.on(
          "UserTyping",
          (data: { userId: string; fileName: string; isTyping: boolean }) => {
            if (isActive) {
              setState((prev) => ({
                ...prev,
                typingUsers: data.isTyping
                  ? [
                      ...prev.typingUsers.filter(
                        (u) => u.userId !== data.userId
                      ),
                      { userId: data.userId, fileName: data.fileName },
                    ]
                  : prev.typingUsers.filter((u) => u.userId !== data.userId),
              }));
            }
          }
        );

        connection.on("UserChangedFile", (fileView: FileViewMessage) => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              participants: prev.participants.map((p) =>
                p.userId === fileView.userId
                  ? {
                      ...p,
                      currentCursor: {
                        ...p.currentCursor,
                        fileName: fileView.fileName,
                      } as CursorPosition,
                    }
                  : p
              ),
            }));
          }
        });

        connection.on("Error", (errorMessage: string) => {
          if (isActive) {
            setState((prev) => ({
              ...prev,
              error: errorMessage,
            }));
          }
        });

        await connection.start();
        connectionRef.current = connection;

        if (isActive) {
          setState((prev) => ({
            ...prev,
            connectionState: HubConnectionState.Connected,
            isConnected: true,
            error: null,
          }));

          try {
            console.log("🔵 Calling JoinSession:", {
              sessionId,
              userId: currentUser.id,
              userName: currentUser.name,
              avatarUrl: currentUser.avatarUrl || "",
            });

            await connection.invoke(
              "JoinSession",
              sessionId,
              currentUser.id,
              currentUser.name,
              currentUser.avatarUrl || ""
            );

            console.log("🔵 JoinSession call completed successfully");
          } catch (error) {
            console.error("Failed to join session:", error);
            setState((prev) => ({
              ...prev,
              error:
                error instanceof Error
                  ? error.message
                  : "Failed to join session",
            }));
          }
        }
      } catch (error) {
        console.error("Failed to initialize SignalR connection:", error);
        if (isActive) {
          setState((prev) => ({
            ...prev,
            error: error instanceof Error ? error.message : "Connection failed",
            isConnected: false,
          }));
        }
      }
    };

    initializeAndJoin();

    return () => {
      isActive = false;
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, [
    sessionId,
    currentUser.id,
    currentUser.name,
    currentUser.avatarUrl,
    onCurrentFileReceived,
  ]);

  const joinSession = useCallback(async () => {
    if (connectionRef.current?.state === HubConnectionState.Connected) {
      try {
        await connectionRef.current.invoke(
          "JoinSession",
          sessionId,
          currentUser.id,
          currentUser.name,
          currentUser.avatarUrl || ""
        );
      } catch (error) {
        console.error("Failed to join session:", error);
        setState((prev) => ({
          ...prev,
          error:
            error instanceof Error ? error.message : "Failed to join session",
        }));
      }
    }
  }, [sessionId, currentUser.id, currentUser.name, currentUser.avatarUrl]);

  const leaveSession = useCallback(async () => {
    if (connectionRef.current?.state === HubConnectionState.Connected) {
      try {
        await connectionRef.current.invoke("LeaveSession", sessionId);
      } catch (error) {
        console.error("Failed to leave session:", error);
      }
    }
  }, [sessionId]);

  const sendCursorPosition = useCallback(
    async (position: CursorPosition) => {
      if (connectionRef.current?.state === HubConnectionState.Connected) {
        try {
          await connectionRef.current.invoke(
            "UpdateCursor",
            sessionId,
            currentUser.id,
            position
          );
        } catch (error) {
          console.error("Failed to send cursor position:", error);
        }
      }
    },
    [sessionId, currentUser.id]
  );

  const sendComment = useCallback(
    async (comment: Omit<LiveComment, "id" | "createdAt">) => {
      if (connectionRef.current?.state === HubConnectionState.Connected) {
        try {
          const fullComment: LiveComment = {
            id: "",
            createdAt: new Date().toISOString(),
            ...comment,
          };
          await connectionRef.current.invoke(
            "SendComment",
            sessionId,
            fullComment
          );
        } catch (error) {
          console.error("Failed to send comment:", error);
        }
      }
    },
    [sessionId]
  );

  const updateComment = useCallback(
    async (comment: LiveComment) => {
      if (connectionRef.current?.state === HubConnectionState.Connected) {
        try {
          await connectionRef.current.invoke(
            "UpdateComment",
            sessionId,
            comment
          );
        } catch (error) {
          console.error("Failed to update comment:", error);
        }
      }
    },
    [sessionId]
  );

  const deleteComment = useCallback(
    async (commentId: string) => {
      if (connectionRef.current?.state === HubConnectionState.Connected) {
        try {
          await connectionRef.current.invoke(
            "DeleteComment",
            sessionId,
            commentId
          );
        } catch (error) {
          console.error("Failed to delete comment:", error);
        }
      }
    },
    [sessionId]
  );

  const resolveComment = useCallback(
    async (commentId: string, isResolved: boolean) => {
      if (connectionRef.current?.state === HubConnectionState.Connected) {
        try {
          await connectionRef.current.invoke(
            "ResolveComment",
            sessionId,
            commentId,
            isResolved
          );
        } catch (error) {
          console.error("Failed to resolve comment:", error);
        }
      }
    },
    [sessionId]
  );

  const addCommentReply = useCallback(
    async (
      commentId: string,
      reply: Omit<CommentReply, "id" | "createdAt">
    ) => {
      if (connectionRef.current?.state === HubConnectionState.Connected) {
        try {
          const fullReply: CommentReply = {
            id: "",
            createdAt: new Date().toISOString(),
            ...reply,
          };
          await connectionRef.current.invoke(
            "AddCommentReply",
            sessionId,
            commentId,
            fullReply
          );
        } catch (error) {
          console.error("Failed to add comment reply:", error);
        }
      }
    },
    [sessionId]
  );

  const notifyTyping = useCallback(
    async (fileName: string, isTyping: boolean) => {
      if (connectionRef.current?.state === HubConnectionState.Connected) {
        try {
          await connectionRef.current.invoke(
            "NotifyTyping",
            sessionId,
            currentUser.id,
            fileName,
            isTyping
          );
        } catch (error) {
          console.error("Failed to notify typing:", error);
        }
      }
    },
    [sessionId, currentUser.id]
  );

  const changeFile = useCallback(
    async (fileName: string) => {
      if (connectionRef.current?.state === HubConnectionState.Connected) {
        try {
          await connectionRef.current.invoke(
            "ChangeFile",
            sessionId,
            currentUser.id,
            fileName
          );
        } catch (error) {
          console.error("Failed to notify file change:", error);
        }
      }
    },
    [sessionId, currentUser.id]
  );

  const setCurrentFile = useCallback(
    async (fileName: string, fileContent: string, fileLanguage: string) => {
      if (connectionRef.current?.state === HubConnectionState.Connected) {
        try {
          await connectionRef.current.invoke(
            "SetCurrentFile",
            sessionId,
            fileName,
            fileContent,
            fileLanguage
          );
        } catch (error) {
          console.error("Failed to set current file:", error);
        }
      }
    },
    [sessionId]
  );

  return {
    ...state,
    joinSession,
    leaveSession,
    sendCursorPosition,
    sendComment,
    updateComment,
    deleteComment,
    resolveComment,
    addCommentReply,
    notifyTyping,
    changeFile,
    setCurrentFile,
  };
};
