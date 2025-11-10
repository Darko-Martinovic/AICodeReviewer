import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react-swc";

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "VITE_");
  const apiBaseUrl = env.VITE_API_BASE_URL || "http://localhost:8001/api";
  const proxyTarget = apiBaseUrl.replace("/api", "");

  return {
    plugins: [react()],
    base: "./", // Use relative paths for assets
    define: {
      "import.meta.env.VITE_API_BASE_URL": JSON.stringify(apiBaseUrl),
    },
    server: {
      port: 5174,
      strictPort: true,
      // https: true, // Uncomment this line if you want to serve the React app over HTTPS as well
      proxy: {
        "/api": {
          target: proxyTarget,
          changeOrigin: true,
          secure: false, // Allow self-signed certificates in development
        },
      },
    },
  };
});
