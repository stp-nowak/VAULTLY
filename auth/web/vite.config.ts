import tailwindcss from '@tailwindcss/vite';
import { tanstackRouter } from '@tanstack/router-plugin/vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';

// The SPA's web server proxies the Identity API paths so every call is same-origin and cookies set by the API are
// first-party: nginx in the Docker image (nginx.conf.template), Vite here in development.
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
