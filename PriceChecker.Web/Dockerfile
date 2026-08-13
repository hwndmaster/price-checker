# ---- Build Stage ----
FROM node:22-alpine AS build
WORKDIR /app

ENV PNPM_STORE_PATH=/pnpm/store

# Install dependencies
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc ./
# pnpm >=11.9 ignores env-var credentials in the project .npmrc, so the token goes
# into the user-level ~/.npmrc; written and removed within one RUN so it never
# persists in an image layer.
RUN --mount=type=cache,id=pricechecker-web-pnpm-store,target=/pnpm/store \
    --mount=type=secret,id=atom_pkg_access_token \
    sh -eu -c 'token="$(cat /run/secrets/atom_pkg_access_token)"; [ -n "$token" ] || { echo "ATOM_PKG_ACCESS_TOKEN is required." >&2; exit 1; }; printf "//npm.pkg.github.com/:_authToken=%s\n" "$token" > "$HOME/.npmrc"; corepack enable; pnpm config set store-dir "$PNPM_STORE_PATH"; pnpm fetch --frozen-lockfile; pnpm install --frozen-lockfile --offline --prod=false; rm -f "$HOME/.npmrc"'

# Copy source and build
COPY . .
RUN pnpm run build

# ---- Serve Stage ----
FROM nginx:alpine AS runtime
COPY --from=build /app/build /usr/share/nginx/html

# Optional: custom nginx config for React Router (handles page refresh)
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 8032
