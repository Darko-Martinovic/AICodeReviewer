import React, { useState } from "react";
import { X, Users, Copy, Check } from "lucide-react";
import styles from "./JoinSessionModal.module.css";

interface JoinSessionModalProps {
  isOpen: boolean;
  onClose: () => void;
  onJoinSession: (sessionId: string, userName: string) => void;
  currentSessionId?: string;
}

export const JoinSessionModal: React.FC<JoinSessionModalProps> = ({
  isOpen,
  onClose,
  onJoinSession,
  currentSessionId,
}) => {
  const [sessionId, setSessionId] = useState("");
  const [userName, setUserName] = useState("");
  const [isJoining, setIsJoining] = useState(false);
  const [copied, setCopied] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!sessionId.trim() || !userName.trim()) return;

    setIsJoining(true);
    try {
      await onJoinSession(sessionId.trim(), userName.trim());
      onClose();
    } catch (error) {
      console.error("Failed to join session:", error);
    } finally {
      setIsJoining(false);
    }
  };

  const copySessionId = async () => {
    if (currentSessionId) {
      try {
        await navigator.clipboard.writeText(currentSessionId);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
      } catch (error) {
        console.error("Failed to copy session ID:", error);
      }
    }
  };

  if (!isOpen) return null;

  return (
    <div className={styles.overlay}>
      <div className={styles.modal}>
        <div className={styles.header}>
          <div className={styles.titleContainer}>
            <Users className={styles.icon} size={20} />
            <h2 className={styles.title}>Join Collaboration Session</h2>
          </div>
          <button
            onClick={onClose}
            className={styles.closeButton}
            type="button"
          >
            <X size={18} />
          </button>
        </div>

        <div className={styles.content}>
          {currentSessionId && (
            <div className={styles.currentSession}>
              <h3 className={styles.sectionTitle}>Current Session</h3>
              <div className={styles.sessionIdContainer}>
                <code className={styles.sessionId}>{currentSessionId}</code>
                <button
                  onClick={copySessionId}
                  className={styles.copyButton}
                  title="Copy session ID"
                >
                  {copied ? (
                    <Check size={16} className={styles.checkIcon} />
                  ) : (
                    <Copy size={16} />
                  )}
                </button>
              </div>
              <p className={styles.shareText}>
                Share this session ID with team members to invite them to
                collaborate
              </p>
              <div className={styles.divider} />
            </div>
          )}

          <form onSubmit={handleSubmit} className={styles.form}>
            <h3 className={styles.sectionTitle}>Join Another Session</h3>

            <div className={styles.inputGroup}>
              <label htmlFor="sessionId" className={styles.label}>
                Session ID
              </label>
              <input
                id="sessionId"
                type="text"
                value={sessionId}
                onChange={(e) => setSessionId(e.target.value)}
                placeholder="Enter session ID (e.g., repo-name-12345678)"
                className={styles.input}
                required
              />
            </div>

            <div className={styles.inputGroup}>
              <label htmlFor="userName" className={styles.label}>
                Your Name
              </label>
              <input
                id="userName"
                type="text"
                value={userName}
                onChange={(e) => setUserName(e.target.value)}
                placeholder="Enter your display name"
                className={styles.input}
                required
              />
            </div>

            <div className={styles.actions}>
              <button
                type="button"
                onClick={onClose}
                className={styles.cancelButton}
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={!sessionId.trim() || !userName.trim() || isJoining}
                className={styles.joinButton}
              >
                {isJoining ? (
                  <>
                    <div className={styles.spinner} />
                    Joining...
                  </>
                ) : (
                  "Join Session"
                )}
              </button>
            </div>
          </form>

          <div className={styles.instructions}>
            <h4 className={styles.instructionsTitle}>How to collaborate:</h4>
            <ul className={styles.instructionsList}>
              <li>Get a session ID from a team member who started a review</li>
              <li>Enter the session ID and your name above</li>
              <li>Click "Join Session" to start collaborating</li>
              <li>
                You'll see live cursors, comments, and can participate in
                real-time
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};
