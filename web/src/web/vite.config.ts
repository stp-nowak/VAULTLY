import { tanstackRouter } from '@tanstack/router-plugin/vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';

// The SPA talks only to the web API (web/src/api) on the same origin. In development Vite proxies /api there so the
// HttpOnly session cookie is first-party for http://localhost:5173.
const apiProxyTarget = process.env.API_PROXY_TARGET ?? 'http://localhost:5180';

export default defineConfig({
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': { target: apiProxyTarget, xfwd: true },
    },
  },
  plugins: [
    tanstackRouter({
      autoCodeSplitting: true,
      // FSD: routing is app-layer wiring, route files only bind URLs to pages.
      routesDirectory: './src/app/routes',
      generatedRouteTree: './src/app/routeTree.gen.ts',
    }),
    react(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
});
