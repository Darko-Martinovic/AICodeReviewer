// Environment configuration for the client application
// API base URL is configured via .env.development or .env.production
// Fallback is defined in vite.config.ts
export const config = {
  api: {
    baseUrl: import.meta.env.VITE_API_BASE_URL,
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
