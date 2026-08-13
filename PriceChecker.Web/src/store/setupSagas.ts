import { SagaWatcher, SagaHandlingType } from "@hwndmaster/atom-react-redux";
import * as agentsSagas from "./agents/sagas";
import * as productsSagas from "./products/sagas";
import * as scansSagas from "./scans/sagas";
import * as agents from "./agents";
import * as products from "./products";
import * as scans from "./scans";

const agentsWatchers: SagaWatcher[] = [
    { handlingType: SagaHandlingType.TakeLatest, action: agents.Actions.fetchAgents, saga: agentsSagas.fetchAgentsSaga },
    { handlingType: SagaHandlingType.TakeLatest, action: agents.Actions.saveAgent, saga: agentsSagas.saveAgentSaga },
    { handlingType: SagaHandlingType.TakeLatest, action: agents.Actions.deleteAgent, saga: agentsSagas.deleteAgentSaga },
];

const productsWatchers: SagaWatcher[] = [
    { handlingType: SagaHandlingType.TakeLatest, action: products.Actions.fetchProductOverviews, saga: productsSagas.fetchProductOverviewsSaga },
    { handlingType: SagaHandlingType.TakeLatest, action: products.Actions.fetchProduct, saga: productsSagas.fetchProductSaga },
    { handlingType: SagaHandlingType.TakeEvery, action: products.Actions.fetchPrices, saga: productsSagas.fetchPricesSaga },
    { handlingType: SagaHandlingType.TakeLatest, action: products.Actions.saveProduct, saga: productsSagas.saveProductSaga },
    { handlingType: SagaHandlingType.TakeLatest, action: products.Actions.deleteProduct, saga: productsSagas.deleteProductSaga },
    { handlingType: SagaHandlingType.TakeLatest, action: products.Actions.dropPrices, saga: productsSagas.dropPricesSaga },
];

const scansWatchers: SagaWatcher[] = [
    { handlingType: SagaHandlingType.TakeLatest, action: scans.Actions.scanAllProducts, saga: scansSagas.scanAllProductsSaga },
    { handlingType: SagaHandlingType.TakeEvery, action: scans.Actions.scanProduct, saga: scansSagas.scanProductSaga },
    { handlingType: SagaHandlingType.TakeLatest, action: scans.Actions.fetchProgress, saga: scansSagas.fetchProgressSaga },
];

export const domainWatchers: SagaWatcher[] = [
    ...agentsWatchers,
    ...productsWatchers,
    ...scansWatchers,
];
