import tailwindcss from '@tailwindcss/vite';
import { tanstackRouter } from '@tanstack/router-plugin/vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';

// In production the Identity API (auth/src) serves this SPA's `dist/` itself, so every call is same-origin.
// In development Vite proxies the Identity API paths so cookies set by the API are first-party for
// http://localhost:5174, exactly like in production.
const authApiProxyTarget = process.env.AUTH_API_PROXY_TARGET ?? 'http://localhost:5096';

const authApiPaths = ['/api', '/authorize', '/google', '/logout', '/token', '/revoke', '/.well-known'];

export default defineConfig({
  server: {
    port: 5174,
    strictPort: true,
    proxy: Object.fromEntries(authApiPaths.map((path) => [path, { target: authApiProxyTarget, xfwd: true }])),
  },
  plugins: [
    tanstackRouter({
      autoCodeSplitting: true,
      // FSD: routing is app-layer wiring, route files only bind URLs to pages.
      routesDirectory: './src/app/routes',
      generatedRouteTree: './src/app/routeTree.gen.ts',
    }),
    react(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
});
