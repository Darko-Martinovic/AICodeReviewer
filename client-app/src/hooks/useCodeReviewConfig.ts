import { useState, useEffect } from "react";
import { configApi, type CodeReviewConfig } from "../services/api";

export function useCodeReviewConfig() {
  const [config, setConfig] = useState<CodeReviewConfig>({
    maxFilesToReview: 3,
    maxIssuesInSummary: 3,
    showTokenMetrics: true, // Default to true
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchConfig = async () => {
      try {
        setLoading(true);
        const response = await configApi.getCodeReviewConfig();
        setConfig(response.data);
        setError(null);
      } catch (err) {
        console.error("Failed to fetch code review config:", err);
        setError("Failed to load configuration");
        // Keep default values if fetch fails
      } finally {
        setLoading(false);
      }
    };

    fetchConfig();
  }, []);

  return { config, loading, error };
}
