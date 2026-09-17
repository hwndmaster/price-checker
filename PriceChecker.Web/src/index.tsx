import React from "react";
import { createRoot } from "react-dom/client";
import { setNotificationService } from "@hwndmaster/atom-web-core";
import { setupAtomTelemetry } from "@hwndmaster/atom-web-telemetry";
import { toastService } from "@hwndmaster/atom-react-prime";
import App from "./App";
import * as serviceWorker from "./serviceWorker";

// The exporter posts to <origin>/otlp, which the published image's nginx always proxies on to the
// dashboard. In development that route exists only when the app host started the dev server and handed
// it the upstream to proxy to; without it every export would land on a 404 and be retried forever.
//
// Before rendering, so a failure during start-up is still reported.
const otlpUpstream: unknown = import.meta.env.VITE_OTLP_UPSTREAM;
const isTelemetrySinkReachable = import.meta.env.PROD === true
    || (typeof otlpUpstream === "string" && otlpUpstream.length > 0);

if (isTelemetrySinkReachable) {
    setupAtomTelemetry({ serviceName: "pricechecker-web", environment: import.meta.env.MODE });
}

setNotificationService(toastService);

const root = createRoot(document.getElementById("root")!);
root.render(
    <React.StrictMode>
        <App />
    </React.StrictMode>
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
