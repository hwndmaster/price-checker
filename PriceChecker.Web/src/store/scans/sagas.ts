import { put } from "redux-saga/effects";
import { callApi, type SagaGenerator } from "@hwndmaster/atom-react-redux";
import apiClient from "@/api/apiAxios";
import ScanProgress from "@/models/scanProgress";
import * as scansActions from "./actions";
import * as actionsInternal from "./actionsInternal";

/**
 * Triggers the scan of all products. The progress arrives over the scan hub.
 */
export function* scanAllProductsSaga(): SagaGenerator {
    yield* callApi(() => apiClient().scans.all()).invoke();
}

/**
 * Triggers the scan of a single product. The progress arrives over the scan hub.
 */
export function* scanProductSaga(action: ReturnType<typeof scansActions.scanProduct>): SagaGenerator {
    yield* callApi(() => apiClient().scans.product(action.payload)).invoke();
}

/**
 * Fetches the current scan progress from the API. Used at startup and on hub reconnects,
 * the live updates otherwise arrive over the scan hub.
 */
export function* fetchProgressSaga(): SagaGenerator {
    const progress: ScanProgress | null = yield* callApi(() => apiClient().scans.progress())
        .fetch((dto) => dto as ScanProgress);
    if (progress != null) {
        yield put(actionsInternal.setProgress(progress));
    }
}
