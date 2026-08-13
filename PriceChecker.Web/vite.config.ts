import path from "path";
import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import { visualizer } from "rollup-plugin-visualizer";

const ENV_PREFIX = ["VITE_", "SERVER_"];

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), ENV_PREFIX);
    const isBundleAnalyzeEnabled = process.env.BUNDLE_ANALYZE === "true";

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
            open: env.SERVER_OPEN_BROWSER === "true"
        },
        build: {
            outDir: "build"
        }
    }
});
