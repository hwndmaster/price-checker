import { createAppStore, SagaHandlingType } from "@hwndmaster/atom-react-redux";
import AppState from "./appState";

import { domainWatchers } from "./setupSagas";

import agentsReducer from "./agents/reducers";
import productsReducer from "./products/reducers";
import scansReducer from "./scans/reducers";

const {
    setupStore,
    getStore,
    setStore,
    getPersistor,
    useAppDispatch,
    useAppSelector,
    watchApplication,
    applicationWatchers,
} = createAppStore<AppState>({
    domainReducers: {
        agents: agentsReducer,
        products: productsReducer,
        scans: scansReducer,
    },
    domainWatchers,
    persistKey: "root",
    // v2 dropped the settings slice and products.editedProduct from the persisted shape.
    persistVersion: 2,
    persistBlacklist: ["common", "scans"],
});

export type AppStore = ReturnType<typeof getStore>;
export type AppDispatch = AppStore["dispatch"];
export {
    setupStore,
    getStore,
    setStore,
    getPersistor,
    useAppDispatch,
    useAppSelector,
    watchApplication,
    applicationWatchers,
    SagaHandlingType,
};
