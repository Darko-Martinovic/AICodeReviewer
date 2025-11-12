import { useEffect, useRef, useState } from "react";
import { collaborationApi } from "../services/collaborationApi";

interface UseSessionAutoJoinProps {
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
  enabled: boolean; // Only connect when enabled
}

/**
 * This hook checks if there's an active file in the session via REST API
 * without joining the SignalR session. It's used for auto-navigation.
 */
export const useSessionAutoJoin = ({
  sessionId,
  currentUser,
  onCurrentFileReceived,
  enabled,
}: UseSessionAutoJoinProps) => {
  const hasChecked = useRef(false);
  const [isChecking, setIsChecking] = useState(false);
  const callbackRef = useRef(onCurrentFileReceived);

  // Update the callback ref when it changes, but don't trigger effect re-run
  useEffect(() => {
    callbackRef.current = onCurrentFileReceived;
  }, [onCurrentFileReceived]);

  useEffect(() => {
    if (!enabled || hasChecked.current || !callbackRef.current) {
      return;
    }

    hasChecked.current = true;
    setIsChecking(true);

    const checkSessionState = async () => {
      try {
        console.log("🔍 Checking session state for:", sessionId);

        // Use REST API to check session state without joining
        const session = await collaborationApi.getSession(sessionId);

        console.log("✅ Session found:", session);
        console.log("🔍 Step 1: Checking if session exists...");

        if (session) {
          console.log("🔍 Step 2: Session exists, checking current file...");

          console.log("🔍 Step 3: Session object type:", typeof session);
          console.log(
            "🔍 Step 4: Session keys:",
            session ? Object.keys(session).join(", ") : "null"
          );

          try {
            // Check each property separately to avoid circular reference issues
            console.log("🔍 Step 5: Checking currentFileName...");
            const fileName = session.currentFileName;
            console.log(
              "🔍 Step 6: fileName =",
              fileName,
              "type:",
              typeof fileName
            );

            console.log("🔍 Step 7: Checking currentFileContent...");
            const fileContent = session.currentFileContent;
            console.log(
              "🔍 Step 8: fileContent length =",
              fileContent ? fileContent.length : 0
            );

            console.log("🔍 Step 9: Checking currentFileLanguage...");
            const fileLanguage = session.currentFileLanguage;
            console.log("🔍 Step 10: fileLanguage =", fileLanguage);

            console.log("🔍 Step 11: Checking callback...");
            console.log(
              "🔍 Step 12: onCurrentFileReceived exists?",
              !!callbackRef.current
            );

            if (fileName && fileContent && callbackRef.current) {
              console.log(
                "✅ Step 13: All conditions met, calling callback with:",
                fileName
              );
              callbackRef.current({
                fileName: fileName,
                fileContent: fileContent,
                fileLanguage: fileLanguage || "text",
              });
              console.log("✅ Step 14: Callback completed successfully");
            } else {
              console.log("⚠️ Step 13: Missing required data:", {
                hasFileName: !!fileName,
                hasContent: !!fileContent,
                hasCallback: !!callbackRef.current,
              });
            }
          } catch (innerError) {
            console.error("❌ Error at step processing:", innerError);
          }
        } else {
          console.log("⚠️ Response data is null");
        }

        setIsChecking(false);
      } catch (error) {
        console.log("⚠️ Session not found or error:", error);
        setIsChecking(false);
      }
    };

    checkSessionState();
  }, [sessionId, currentUser.id, enabled]); // Removed onCurrentFileReceived from deps - stored in ref

  return { isChecking };
};
