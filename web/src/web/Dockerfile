# Web SPA served by nginx, which also proxies /api to the web API so the session cookie stays same-origin
# (the role the Vite dev server plays in development).
# Build context: repository root — the SPA is a pnpm workspace package that depends on packages/ui.
#   docker build -f web/src/web/Dockerfile -t vaultly-web-spa .
# Runtime: API_PROXY_TARGET = base URL of the web API, e.g. http://web-api:8080

FROM node:26-alpine AS build
WORKDIR /repo
RUN corepack enable
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
COPY packages/ui/package.json packages/ui/
COPY auth/web/package.json auth/web/
COPY web/src/web/package.json web/src/web/
RUN pnpm install --frozen-lockfile --filter "@vaultly/web..."
COPY packages/ui packages/ui
COPY web/src/web web/src/web
RUN pnpm --filter @vaultly/web build

FROM nginxinc/nginx-unprivileged:stable-alpine
ENV NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1
COPY web/src/web/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /repo/web/src/web/dist /usr/share/nginx/html
EXPOSE 8080
