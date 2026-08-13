import React from "react";
import { Provider } from "react-redux";
import { PersistGate } from "redux-persist/integration/react";
import { LoadingSpinner } from "@hwndmaster/atom-react-redux";
import { ToastProvider } from "@hwndmaster/atom-react-prime";
import { ConfirmDialog, PrimeReactProvider } from "@/primereact";

import { setupAxiosInstances } from "./api/setup";
import { startScanHubConnection } from "./store/scans/messages";
import RootComponent from "./RootComponent";
import { getPersistor } from "./store";
import { getStore } from "./store/setup";
import LoadingTargets from "./shared/loadingTargets";

// Atom package styles are emitted as separate CSS bundles and must be imported once at app root.
import "@hwndmaster/atom-react-core/styles.css";
import "@hwndmaster/atom-react-prime/styles.css";
import "@hwndmaster/atom-react-redux/styles.css";
import "primereact/resources/themes/viva-dark/theme.css";
import "primeicons/primeicons.css";
import "./styles/main.scss";

setupAxiosInstances();
void startScanHubConnection();

const App: React.FC = () => {
    return (
        <Provider store={getStore()}>
            <PersistGate loading={null} persistor={getPersistor()}>
                <PrimeReactProvider>
                    <ConfirmDialog />
                    <ToastProvider>
                        <LoadingSpinner target={LoadingTargets.WholePage}>
                            <RootComponent />
                        </LoadingSpinner>
                    </ToastProvider>
                </PrimeReactProvider>
            </PersistGate>
        </Provider>
    );
};

export default App;
