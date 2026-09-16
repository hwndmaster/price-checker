import React from "react";
import { createRoot } from "react-dom/client";
import { setNotificationService } from "@hwndmaster/atom-web-core";
import { setupAtomTelemetry } from "@hwndmaster/atom-web-telemetry";
import { toastService } from "@hwndmaster/atom-react-prime";
import App from "./App";
import * as serviceWorker from "./serviceWorker";

// Before rendering, so a failure during start-up is still reported.
setupAtomTelemetry({ serviceName: "pricechecker-web", environment: import.meta.env.MODE });

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
