import path from "path";
import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import { visualizer } from "rollup-plugin-visualizer";

const ENV_PREFIX = ["VITE_", "SERVER_"];

/** Base path the browser's OTLP exporter posts to, mirroring the `location /otlp/` block in nginx.conf. */
const OTLP_BASE_PATH = "/otlp";

/** Where the dev server forwards {@link OTLP_BASE_PATH} to. Set by the app host; see the proxy below. */
const OTLP_UPSTREAM_VAR = "VITE_OTLP_UPSTREAM";

/**
 * Headers the OTLP endpoint expects, read from the standard `key=value,key=value` variable Aspire injects
 * into this process. Locally the dashboard ingests under an api key and rejects an unauthenticated export
 * with a 401, so the dev server has to present it — server-side, rather than shipping a credential to
 * every browser that loads the app. Empty wherever the endpoint takes anonymous exports, as the published
 * image's does.
 */
function resolveOtlpHeaders(): Record<string, string> {
    const raw = process.env.OTEL_EXPORTER_OTLP_HEADERS?.trim();
    if (raw == null || raw.length === 0) {
        return {};
    }

    return Object.fromEntries(
        raw.split(",")
            .map(pair => pair.split(/=(.*)/s, 2))
            .filter(([name, value]) => name != null && name.trim().length > 0 && value != null)
            .map(([name, value]) => {
                // Values arrive percent-encoded. A malformed one is passed through rather than thrown:
                // wiring up telemetry must never be what stops the dev server from starting.
                try {
                    return [name.trim(), decodeURIComponent(value.trim())];
                } catch {
                    return [name.trim(), value.trim()];
                }
            })
    );
}

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), ENV_PREFIX);
    const isBundleAnalyzeEnabled = process.env.BUNDLE_ANALYZE === "true";

    // Loaded on its own rather than out of `env` above, so that it is found whatever ENV_PREFIX a repo
    // happens to use, and resolved the same way the browser resolves import.meta.env — from the
    // environment as well as from .env. The app host is what sets it, so a dev server started on its own
    // gets no proxy and, with the variable equally absent from the bundle, an SPA that never starts the
    // exporter. One variable drives both ends, so the browser cannot export into a route that is not there.
    const otlpUpstream = loadEnv(mode, process.cwd(), OTLP_UPSTREAM_VAR)[OTLP_UPSTREAM_VAR]?.trim();

    const plugins = [
        react()
    ];

    if (isBundleAnalyzeEnabled) {
        plugins.push(
            visualizer({
                filename: "bundle-stats.html",
                template: "treemap",
                gzipSize: true,
                brotliSize: true,
                emitFile: true
            })
        );

        plugins.push(
            visualizer({
                filename: "bundle-stats.json",
                template: "raw-data",
                gzipSize: true,
                brotliSize: true,
                emitFile: true
            })
        );
    }

    return {
        // A path, never an absolute URL: the build bakes it into every asset link in index.html, and the
        // published image is opened under whatever address its server has. "http://localhost:<port>"
        // here works on the machine that built it and nowhere else.
        base: env.VITE_BASE_URL || "/",
        css: {
            preprocessorOptions: {
                scss: {
                    api: "modern",
                    additionalData: `@use "@/styles/_variables" as *; @use "@/styles/_mixins" as *;`
                }
            }
        },
        plugins,
        resolve: {
            tsconfigPaths: true,
            alias: {
                "@": path.resolve(__dirname, "./src")
            }
        },
        server: {
            port: 5081,
            open: env.SERVER_OPEN_BROWSER === "true",
            // The dashboard ingests OTLP under /v1/..., so the prefix is rewritten away exactly as the
            // nginx block does.
            proxy: otlpUpstream
                ? {
                    [OTLP_BASE_PATH]: {
                        target: otlpUpstream,
                        changeOrigin: true,
                        headers: resolveOtlpHeaders(),
                        rewrite: (requestPath: string) => requestPath.replace(new RegExp(`^${OTLP_BASE_PATH}`), "")
                    }
                }
                : undefined
        },
        build: {
            outDir: "build"
        }
    }
});
