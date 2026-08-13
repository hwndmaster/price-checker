import { defineConfig, mergeConfig } from "vitest/config";
import viteConfig from "./vite.config";

export default defineConfig((env) => {
  const config = viteConfig(env);

  return mergeConfig(config, defineConfig({
    test: {
      globals: true,
      setupFiles: ["./setupTests.ts"],
      environment: "jsdom",
      server: {
        deps: {
          inline: [
            "@hwndmaster/atom-api-core",
            "@hwndmaster/atom-react-core",
            "@hwndmaster/atom-react-prime",
            "@hwndmaster/atom-react-redux",
            "@hwndmaster/atom-web-core",
          ],
          fallbackCJS: true,
        }
      },
      coverage: {
        provider: "v8",
        reporter: ["text", "lcov"],
        include: ["src/**/*.{ts,tsx}"],
        exclude: [
          "src/**/*.d.ts",
          "src/**/*.test.{ts,tsx}",
          "src/**/__tests__/**",
          "src/**/tests/**",
        ],
      }
    },
  }));
});
