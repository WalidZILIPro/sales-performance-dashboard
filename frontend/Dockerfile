# syntax=docker/dockerfile:1

# ---- deps: installed from the lockfile only, so this layer is cached until dependencies change ----
FROM node:20-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

# ---- source ----
FROM deps AS source
COPY . .

# ---- test: `docker build --target test frontend` type-checks and runs the test suite ----
FROM source AS test
RUN npm run typecheck && npm test

# ---- build ----
FROM source AS build
RUN npm run build

# ---- runtime: static files served by nginx, which also proxies /api to the backend ----
FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=10s --timeout=3s --retries=3 CMD wget -qO- http://127.0.0.1/healthz || exit 1
