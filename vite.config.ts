import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react-swc";
import { defineConfig } from "vite";
import { resolve } from 'path'

const projectRoot = process.env.PROJECT_ROOT || import.meta.dirname

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  build: {
    outDir: process.env.OUTPUT_DIR || 'dist'
  },
  define: {
    GITHUB_RUNTIME_PERMANENT_NAME: JSON.stringify("social-media-commander"),
    BASE_KV_SERVICE_URL: JSON.stringify("/_spark/kv"),
  },
  server: {
    port: 5000,
    hmr: {
      overlay: false,
    },
    cors: {
      origin: /^https?:\/\/(?:(?:[^:]+\.)?localhost|127\.0\.0\.1|\[::1\]|(?:.*\.)?github\.com)(?::\d+)?$/
    },
    watch: {
      ignored: ["**/prd.md", "**.log"],
      awaitWriteFinish: {
        pollInterval: 50,
        stabilityThreshold: 100,
      },
    },
  },
  resolve: {
    alias: {
      '@': resolve(projectRoot, 'src')
    }
  },
  publicDir: 'public',
  assetsInclude: ['**/*.md'],
});