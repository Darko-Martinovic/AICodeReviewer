// Environment configuration for the client application
export const config = {
  api: {
    baseUrl: import.meta.env.VITE_API_BASE_URL || "http://localhost:8001/api",
    timeout: Number(import.meta.env.VITE_API_TIMEOUT) || 10000,
    longTimeout: Number(import.meta.env.VITE_API_LONG_TIMEOUT) || 180000,
  },
} as const;

// For development logging
if (import.meta.env.DEV) {
  console.log("🔧 Client Configuration:", {
    apiBaseUrl: config.api.baseUrl,
    timeout: config.api.timeout,
    longTimeout: config.api.longTimeout,
  });
}
