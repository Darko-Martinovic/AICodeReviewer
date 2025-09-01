import React, { useState, useEffect, useMemo } from "react";
import { useCollaboration } from "../hooks/useCollaboration";
import type { LiveComment, CursorPosition } from "../types/collaboration";
import styles from "./CollaborativeCodeViewer.module.css";

interface CollaborativeCodeViewerProps {
  sessionId: string;
  fileName: string;
  fileContent: string;
  language: string;
  currentUser: {
    id: string;
    name: string;
    avatarUrl?: string;
  };
  onClose?: () => void;
}

const USER_COLORS = [
  "#FF6B6B",
  "#4ECDC4",
  "#45B7D1",
  "#96CEB4",
  "#FECA57",
  "#FF9FF3",
  "#54A0FF",
  "#5F27CD",
  "#00D2D3",
  "#FF9F43",
];

export const CollaborativeCodeViewer: React.FC<
  CollaborativeCodeViewerProps
> = ({ sessionId, fileName, fileContent, language, currentUser, onClose }) => {
  const [selectedLines, setSelectedLines] = useState<Set<number>>(new Set());
  const [commentText, setCommentText] = useState("");
  const [isAddingComment, setIsAddingComment] = useState(false);

  const collaboration = useCollaboration({
    sessionId,
    currentUser,
  });

  const codeLines = useMemo(() => fileContent.split("\n"), [fileContent]);

  // Simple syntax highlighting for better readability
  const highlightLine = (line: string): React.ReactElement => {
    if (!line) return <span> </span>;

    // Use JSX elements instead of dangerouslySetInnerHTML
    const elements: React.ReactNode[] = [];
    let lastIndex = 0;

    // Keywords pattern
    const keywordPattern =
      /\b(class|interface|function|const|let|var|if|else|for|while|return|public|private|protected|static|async|await|new|this|super|extends|implements|namespace|using)\b/g;

    // Comment pattern
    const commentPattern = /(\/\/.*$)/g;

    // String pattern
    const stringPattern = /(["'`].*?["'`])/g;

    // Number pattern
    const numberPattern = /\b(\d+)\b/g;

    // Collect all matches with their types
    const matches: Array<{
      start: number;
      end: number;
      type: string;
      text: string;
    }> = [];

    let match;
    while ((match = keywordPattern.exec(line)) !== null) {
      matches.push({
        start: match.index,
        end: match.index + match[0].length,
        type: "keyword",
        text: match[0],
      });
    }

    keywordPattern.lastIndex = 0;
    while ((match = commentPattern.exec(line)) !== null) {
      matches.push({
        start: match.index,
        end: match.index + match[0].length,
        type: "comment",
        text: match[0],
      });
    }

    commentPattern.lastIndex = 0;
    while ((match = stringPattern.exec(line)) !== null) {
      matches.push({
        start: match.index,
        end: match.index + match[0].length,
        type: "string",
        text: match[0],
      });
    }

    stringPattern.lastIndex = 0;
    while ((match = numberPattern.exec(line)) !== null) {
      matches.push({
        start: match.index,
        end: match.index + match[0].length,
        type: "number",
        text: match[0],
      });
    }

    // Sort matches by start position and remove overlaps
    matches.sort((a, b) => a.start - b.start);
    const nonOverlappingMatches = [];
    let lastEnd = 0;

    for (const match of matches) {
      if (match.start >= lastEnd) {
        nonOverlappingMatches.push(match);
        lastEnd = match.end;
      }
    }

    // Build elements array
    lastIndex = 0;
    nonOverlappingMatches.forEach((match, index) => {
      // Add text before this match
      if (match.start > lastIndex) {
        elements.push(line.substring(lastIndex, match.start));
      }

      // Add the highlighted element
      elements.push(
        <span key={`${index}-${match.type}`} className={styles[match.type]}>
          {match.text}
        </span>
      );

      lastIndex = match.end;
    });

    // Add remaining text
    if (lastIndex < line.length) {
      elements.push(line.substring(lastIndex));
    }

    return <span>{elements.length > 0 ? elements : line}</span>;
  };

  // Map participant colors
  const participantColors = useMemo(() => {
    const colors = new Map<string, string>();
    collaboration.participants.forEach((participant, index) => {
      colors.set(participant.userId, USER_COLORS[index % USER_COLORS.length]);
    });
    return colors;
  }, [collaboration.participants]);

  // Handle cursor movement
  const handleMouseMove = (event: React.MouseEvent<HTMLDivElement>) => {
    const element = event.currentTarget;
    const rect = element.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;

    // Estimate line and column based on position
    const lineHeight = 20; // Approximate line height
    const charWidth = 8; // Approximate character width
    const lineNum = Math.floor(y / lineHeight);
    const column = Math.floor(x / charWidth);

    const position: CursorPosition = {
      fileName,
      lineNumber: lineNum,
      column,
      lastUpdated: new Date().toISOString(),
    };

    collaboration.sendCursorPosition(position);
  };

  // Handle line selection for comments
  const handleLineClick = (lineNumber: number) => {
    setSelectedLines((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(lineNumber)) {
        newSet.delete(lineNumber);
      } else {
        newSet.add(lineNumber);
      }
      return newSet;
    });
  };

  // Add comment to selected lines
  const handleAddComment = async () => {
    if (commentText.trim() && selectedLines.size > 0) {
      const sortedLines = Array.from(selectedLines).sort((a, b) => a - b);
      const comment: Omit<LiveComment, "id" | "createdAt"> = {
        fileName,
        lineNumber: sortedLines[0],
        content: commentText.trim(),
        userId: currentUser.id,
        userName: currentUser.name,
        isResolved: false,
        commentType: "general" as const,
        replies: [],
      };

      await collaboration.sendComment(comment);
      setCommentText("");
      setSelectedLines(new Set());
      setIsAddingComment(false);
    }
  };

  // Get comments for a specific line
  const getCommentsForLine = (lineNumber: number) => {
    return collaboration.comments.filter(
      (comment) => comment.lineNumber === lineNumber
    );
  };

  // Get cursors for current file
  const activeCursors = collaboration.cursors.filter(
    (cursor) =>
      cursor.position.fileName === fileName && cursor.userId !== currentUser.id
  );

  // Notify file change
  useEffect(() => {
    console.log("🔄 File changed in CollaborativeCodeViewer:", fileName);
    console.log("📊 Collaboration state:", {
      sessionId,
      isConnected: collaboration.isConnected,
      participantsCount: collaboration.participants.length,
      cursorsCount: collaboration.cursors.length,
    });
    collaboration.changeFile(fileName);
  }, [fileName, sessionId, collaboration]);

  return (
    <div className={styles.collaborativeViewer}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.fileInfo}>
          <span className={styles.fileName}>{fileName}</span>
          <span className={styles.language}>{language}</span>
        </div>

        <div className={styles.participants}>
          {collaboration.participants.map((participant) => (
            <div
              key={participant.userId}
              className={styles.participant}
              style={{ borderColor: participantColors.get(participant.userId) }}
              title={participant.userName}
            >
              {participant.avatarUrl ? (
                <img
                  src={participant.avatarUrl}
                  alt={participant.userName}
                  className={styles.participantAvatar}
                />
              ) : (
                <div
                  className={styles.participantInitial}
                  style={{
                    backgroundColor: participantColors.get(participant.userId),
                  }}
                >
                  {participant.userName.charAt(0).toUpperCase()}
                </div>
              )}
              {participant.isActive && (
                <div className={styles.activeIndicator} />
              )}
            </div>
          ))}
        </div>

        <div className={styles.actions}>
          <button
            className={styles.commentButton}
            onClick={() => setIsAddingComment(!isAddingComment)}
            disabled={selectedLines.size === 0}
          >
            💬 Comment ({selectedLines.size} lines)
          </button>
          {onClose && (
            <button className={styles.closeButton} onClick={onClose}>
              ✕
            </button>
          )}
        </div>
      </div>

      {/* Debug Panel */}
      <div className={styles.debugPanel}>
        <div className={styles.debugItem}>
          <strong>Session:</strong> {sessionId}
        </div>
        <div className={styles.debugItem}>
          <strong>Connected:</strong> {collaboration.isConnected ? "✅" : "❌"}
        </div>
        <div className={styles.debugItem}>
          <strong>Participants:</strong> {collaboration.participants.length}
        </div>
        <div className={styles.debugItem}>
          <strong>Cursors:</strong> {collaboration.cursors.length}
        </div>
        <div className={styles.debugItem}>
          <strong>Current User:</strong> {currentUser.name} ({currentUser.id})
        </div>
      </div>

      {/* Connection Status */}
      {!collaboration.isConnected && (
        <div className={styles.connectionStatus}>
          {collaboration.connectionState === "Reconnecting" ? (
            <span className={styles.reconnecting}>🔄 Reconnecting...</span>
          ) : (
            <span className={styles.disconnected}>❌ Disconnected</span>
          )}
        </div>
      )}

      {/* Code Viewer */}
      <div className={styles.codeContainer} onMouseMove={handleMouseMove}>
        <div className={styles.lineNumbers}>
          {codeLines.map((_, index) => (
            <div
              key={index}
              className={`${styles.lineNumber} ${
                selectedLines.has(index) ? styles.selected : ""
              }`}
              onClick={() => handleLineClick(index)}
            >
              {index + 1}
            </div>
          ))}
        </div>

        <div className={styles.codeContent}>
          {codeLines.map((line, index) => {
            const lineComments = getCommentsForLine(index);
            const lineCursors = activeCursors.filter(
              (cursor) => cursor.position.lineNumber === index
            );

            return (
              <div key={index} className={styles.codeLine}>
                <div
                  className={`${styles.lineContent} ${
                    selectedLines.has(index) ? styles.selectedLine : ""
                  }`}
                  onClick={() => handleLineClick(index)}
                >
                  <pre className={styles.code}>{highlightLine(line)}</pre>

                  {/* Other users' cursors */}
                  {lineCursors.map((cursor) => (
                    <div
                      key={cursor.userId}
                      className={styles.cursor}
                      style={{
                        left: `${cursor.position.column * 8}px`,
                        borderColor: participantColors.get(cursor.userId),
                      }}
                      title={
                        collaboration.participants.find(
                          (p) => p.userId === cursor.userId
                        )?.userName
                      }
                    >
                      <div
                        className={styles.cursorFlag}
                        style={{
                          backgroundColor: participantColors.get(cursor.userId),
                        }}
                      >
                        {collaboration.participants
                          .find((p) => p.userId === cursor.userId)
                          ?.userName?.charAt(0)}
                      </div>
                    </div>
                  ))}
                </div>

                {/* Comments */}
                {lineComments.length > 0 && (
                  <div className={styles.commentsContainer}>
                    {lineComments.map((comment) => (
                      <div key={comment.id} className={styles.comment}>
                        <div className={styles.commentHeader}>
                          <span className={styles.commentAuthor}>
                            {comment.userName}
                          </span>
                          <span className={styles.commentTime}>
                            {new Date(comment.createdAt).toLocaleTimeString()}
                          </span>
                          {comment.isResolved && (
                            <span className={styles.resolvedBadge}>
                              ✅ Resolved
                            </span>
                          )}
                        </div>
                        <div className={styles.commentContent}>
                          {comment.content}
                        </div>

                        {/* Comment actions */}
                        <div className={styles.commentActions}>
                          <button
                            className={styles.resolveButton}
                            onClick={() =>
                              collaboration.resolveComment(
                                comment.id,
                                !comment.isResolved
                              )
                            }
                          >
                            {comment.isResolved ? "Unresolve" : "Resolve"}
                          </button>
                          {comment.userId === currentUser.id && (
                            <button
                              className={styles.deleteButton}
                              onClick={() =>
                                collaboration.deleteComment(comment.id)
                              }
                            >
                              Delete
                            </button>
                          )}
                        </div>

                        {/* Replies */}
                        {comment.replies.length > 0 && (
                          <div className={styles.replies}>
                            {comment.replies.map((reply) => (
                              <div key={reply.id} className={styles.reply}>
                                <div className={styles.replyHeader}>
                                  <span className={styles.replyAuthor}>
                                    {reply.userName}
                                  </span>
                                  <span className={styles.replyTime}>
                                    {new Date(
                                      reply.createdAt
                                    ).toLocaleTimeString()}
                                  </span>
                                </div>
                                <div className={styles.replyContent}>
                                  {reply.content}
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* Comment Input */}
      {isAddingComment && (
        <div className={styles.commentInput}>
          <div className={styles.commentInputHeader}>
            <span>
              Add comment to lines {Array.from(selectedLines).sort().join(", ")}
            </span>
          </div>
          <textarea
            className={styles.commentTextarea}
            value={commentText}
            onChange={(e) => setCommentText(e.target.value)}
            placeholder="Enter your comment..."
            rows={3}
          />
          <div className={styles.commentInputActions}>
            <button
              className={styles.submitButton}
              onClick={handleAddComment}
              disabled={!commentText.trim()}
            >
              Add Comment
            </button>
            <button
              className={styles.cancelButton}
              onClick={() => {
                setIsAddingComment(false);
                setCommentText("");
              }}
            >
              Cancel
            </button>
          </div>
        </div>
      )}

      {/* Typing Indicators */}
      {collaboration.typingUsers.length > 0 && (
        <div className={styles.typingIndicators}>
          {collaboration.typingUsers
            .filter((user) => user.fileName === fileName)
            .map((user) => {
              const participant = collaboration.participants.find(
                (p) => p.userId === user.userId
              );
              return participant ? (
                <div key={user.userId} className={styles.typingIndicator}>
                  <span style={{ color: participantColors.get(user.userId) }}>
                    {participant.userName} is typing...
                  </span>
                </div>
              ) : null;
            })}
        </div>
      )}
    </div>
  );
};
