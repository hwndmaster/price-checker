import globals from "globals";
import { commonConfig } from "@hwndmaster/atom-eslint-common";
import { stylisticConfig } from "@hwndmaster/atom-eslint-stylistic";
import { reactConfig, reactTestConfig } from "@hwndmaster/atom-eslint-react";

export default [
    {
        ignores: [
            "build/",
            "node_modules/",
            "coverage/",
            "eslint.config.js",
            "vite.config.ts",
            "vitest.config.ts",
            "setupTests.ts",
        ]
    },
    ...commonConfig,
    ...stylisticConfig,
    ...reactConfig,
    {
        files: ["**/*.ts", "**/*.tsx"],
        languageOptions: {
            parserOptions: {
                project: "./tsconfig.eslint.json",
                tsconfigRootDir: import.meta.dirname,
            },
            globals: {
                ...globals.browser,
                ...globals.node,
                ...globals.es2025,
                "React": "readonly",
                "JSX": "readonly",
            }
        },
        rules: {
            "@typescript-eslint/no-unsafe-argument": "warn",
            "@typescript-eslint/no-unsafe-assignment": "off",
            "@typescript-eslint/no-unsafe-call": "off",
            "@typescript-eslint/no-unsafe-member-access": "off",
            "import-x/no-restricted-paths": ["error", {
                zones: [
                    {
                        target: "./src/store/**/!(sagas|sagas.test|messages|apiRequest).{ts,tsx}",
                        from: "./src/api",
                        message: "Only sagas.ts and messages.ts can import from the api/ folder.",
                    },
                    {
                        target: "./src/!(api|store|__tests__)/**",
                        from: "./src/api",
                        message: "The api/ folder can only be accessed from the store/ folder.",
                    },
                    {
                        target: "./src/!(store)/**",
                        from: "./src/store/**/actionsInternal.{ts,tsx}",
                        message: "actionsInternal.ts can only be imported within the store/ folder.",
                    },
                    {
                        target: "./src/!(store|__tests__)/**",
                        from: "./src/store/**/sagas.{ts,tsx}",
                        message: "sagas.ts can only be imported within the store/ folder.",
                    },
                ],
            }],
        },
    },
    {
        ...reactTestConfig[0],
        files: ["**/__tests__/*", "**/serviceWorker.ts"],
        languageOptions: {
            ...reactTestConfig[0].languageOptions,
            parserOptions: {
                project: "./tsconfig.test.json",
                tsconfigRootDir: import.meta.dirname,
            }
        },
    },
    {
        files: ["**/store/**/sagas.ts"],
        rules: {
            "@typescript-eslint/promise-function-async": "off",
        }
    },
    {
        files: ["src/shared/loadingTargets.ts"],
        rules: {
            "@typescript-eslint/prefer-literal-enum-member": "off",
        }
    }
];
