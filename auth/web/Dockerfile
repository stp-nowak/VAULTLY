# Identity login UI served by nginx, which also proxies the Identity API paths so the UI and API stay same-origin
# (the role the Vite dev server plays in development).
# Build context: repository root — the UI is a pnpm workspace package that depends on packages/ui.
#   docker build -f auth/web/Dockerfile -t vaultly-identity-web .
# Runtime: AUTH_API_PROXY_TARGET = base URL of the Identity API, e.g. http://identity-api:8080

FROM node:26-alpine AS build
WORKDIR /repo
# Node 25+ no longer bundles corepack; it provides the pnpm version pinned in package.json "packageManager".
RUN npm install --global corepack@0.36.0 && corepack enable
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
COPY packages/ui/package.json packages/ui/
COPY auth/web/package.json auth/web/
COPY web/src/web/package.json web/src/web/
RUN pnpm install --frozen-lockfile --filter "@vaultly/auth-web..."
COPY packages/ui packages/ui
COPY auth/web auth/web
RUN pnpm --filter @vaultly/auth-web build

FROM nginxinc/nginx-unprivileged:stable-alpine
ENV NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1
COPY auth/web/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /repo/auth/web/dist /usr/share/nginx/html
EXPOSE 8080
