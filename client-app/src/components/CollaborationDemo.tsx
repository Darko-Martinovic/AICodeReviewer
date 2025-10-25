import React, { useState } from "react";
import { Copy, Check } from "lucide-react";
import { CollaborativeCodeViewer } from "./CollaborativeCodeViewer";
import { useSessionAutoJoin } from "../hooks/useSessionAutoJoin";
import styles from "./CollaborationDemo.module.css";

interface CommitFile {
  filename: string;
  content: string;
  language: string;
  status: "added" | "modified" | "deleted";
}

interface CollaborationDemoProps {
  commitSha: string;
  repositoryFullName: string;
  files: CommitFile[];
  currentUser: {
    id: string;
    name: string;
    avatarUrl?: string;
  };
  onFetchFileContent: (commitSha: string, filename: string) => Promise<string>;
}

export const CollaborationDemo: React.FC<CollaborationDemoProps> = ({
  commitSha,
  repositoryFullName,
  files,
  currentUser,
  onFetchFileContent,
}) => {
  const [selectedFile, setSelectedFile] = useState<CommitFile | null>(null);
  const [isCollaborating, setIsCollaborating] = useState(false);
  const [copySuccess, setCopySuccess] = useState(false);
  const [loadingFileContent, setLoadingFileContent] = useState(false);
  const [realFileContent, setRealFileContent] = useState<string>("");

  // Generate session ID from commit and repository
  const sessionId = `${repositoryFullName.replace(
    "/",
    "-"
  )}-${commitSha.substring(0, 8)}`;

  // Handle when receiving current file info from session (auto-join feature)
  const handleCurrentFileReceived = React.useCallback(
    async (fileInfo: {
      fileName: string;
      fileContent: string;
      fileLanguage: string;
    }) => {
      console.log("🔍 handleCurrentFileReceived called:", {
        fileName: fileInfo.fileName,
        hasContent: !!fileInfo.fileContent,
        filesCount: files.length,
        isCollaborating,
      });

      const matchingFile = files.find((f) => f.filename === fileInfo.fileName);

      console.log("🔍 Matching file:", matchingFile);

      if (matchingFile && !isCollaborating) {
        console.log("✅ Auto-opening file from session:", fileInfo.fileName);
        setSelectedFile(matchingFile);
        setRealFileContent(fileInfo.fileContent);
        setIsCollaborating(true);
      } else {
        console.log("❌ Cannot auto-open file:", {
          hasMatchingFile: !!matchingFile,
          isCollaborating,
        });
      }
    },
    [files, isCollaborating]
  );

  // Check if there's an active file in the session and auto-open it
  // This doesn't permanently join the session - just checks and disconnects
  useSessionAutoJoin({
    sessionId,
    currentUser,
    onCurrentFileReceived: handleCurrentFileReceived,
    enabled: !isCollaborating, // Only check when not already collaborating
  });

  const handleCopySessionId = async () => {
    try {
      await navigator.clipboard.writeText(sessionId);
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
    } catch (err) {
      console.error("Failed to copy session ID:", err);
      // Fallback for older browsers
      const textArea = document.createElement("textarea");
      textArea.value = sessionId;
      document.body.appendChild(textArea);
      textArea.select();
      document.execCommand("copy");
      document.body.removeChild(textArea);
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
    }
  };

  const handleStartCollaboration = async (file: CommitFile) => {
    setSelectedFile(file);
    setLoadingFileContent(true);

    try {
      // Fetch real file content
      const content = await onFetchFileContent(commitSha, file.filename);
      setRealFileContent(content);
      setIsCollaborating(true);
    } catch (error) {
      console.error("Failed to fetch file content:", error);
      // Use fallback content if fetch fails
      setRealFileContent(`// Error loading ${file.filename}
// Using fallback content for demo

${file.content}`);
      setIsCollaborating(true);
    } finally {
      setLoadingFileContent(false);
    }
  };

  const handleEndCollaboration = () => {
    setIsCollaborating(false);
    setSelectedFile(null);
    setRealFileContent("");
  };

  if (loadingFileContent && selectedFile) {
    return (
      <div className={styles.collaborationDemo}>
        <div className={styles.header}>
          <h2 className={styles.title}>Loading File Content</h2>
        </div>
        <div className={styles.loadingContainer}>
          <div className={styles.spinner}></div>
          <p>Fetching real content for {selectedFile.filename}...</p>
        </div>
      </div>
    );
  }

  if (isCollaborating && selectedFile) {
    return (
      <CollaborativeCodeViewer
        sessionId={sessionId}
        fileName={selectedFile.filename}
        fileContent={realFileContent}
        language={selectedFile.language}
        currentUser={currentUser}
        onClose={handleEndCollaboration}
      />
    );
  }

  return (
    <div className={styles.collaborationDemo}>
      <div className={styles.header}>
        <h2 className={styles.title}>Real-time Collaboration Demo</h2>
        <div className={styles.sessionInfo}>
          <span className={styles.sessionLabel}>Session ID:</span>
          <code className={styles.sessionId}>{sessionId}</code>
          <button
            onClick={handleCopySessionId}
            className={styles.copyButton}
            title={copySuccess ? "Copied!" : "Copy Session ID"}
          >
            {copySuccess ? (
              <Check className={styles.copyIcon} />
            ) : (
              <Copy className={styles.copyIcon} />
            )}
          </button>
        </div>
      </div>

      <div className={styles.description}>
        <p>
          Select a file below to start a collaborative code review session.
          Multiple users can join the same session using the session ID to:
        </p>
        <ul className={styles.featureList}>
          <li>💻 View live cursor positions of other participants</li>
          <li>💬 Add and reply to line-specific comments</li>
          <li>👥 See who else is actively reviewing the code</li>
          <li>⚡ Get real-time updates as others type and navigate</li>
          <li>✅ Resolve discussions and track progress</li>
        </ul>
      </div>

      <div className={styles.fileList}>
        <h3 className={styles.fileListTitle}>Files in this commit:</h3>
        {files.map((file, index) => (
          <div key={index} className={styles.fileItem}>
            <div className={styles.fileContent}>
              <div className={styles.fileInfo}>
                <span className={styles.fileName}>{file.filename}</span>
              </div>
              <div className={styles.fileMeta}>
                <span className={`${styles.fileStatus} ${styles[file.status]}`}>
                  {file.status}
                </span>
                <span className={styles.fileLanguage}>
                  Language: {file.language}
                </span>
              </div>
            </div>
            <button
              className={styles.collaborateButton}
              onClick={() => handleStartCollaboration(file)}
            >
              🚀 Start Collaboration
            </button>
          </div>
        ))}
      </div>

      <div className={styles.instructions}>
        <div className={styles.instructionsHeader}>
          <h4>How to collaborate:</h4>
        </div>
        <ol className={styles.instructionsList}>
          <li>Share the session ID with other team members</li>
          <li>Click "Start Collaboration" on any file</li>
          <li>Others can join by entering the same session ID</li>
          <li>Click on line numbers to select lines for commenting</li>
          <li>Use the comment button to add feedback</li>
          <li>See real-time cursors and typing indicators</li>
        </ol>
      </div>
    </div>
  );
};
