import { put } from "redux-saga/effects";
import { callApi, type SagaGenerator, withCallback, withLoading, withValidatableCallback } from "@hwndmaster/atom-react-redux";
import apiClient from "@/api/apiAxios";
import { convertAgentApiToModel, convertRecognizedSourceApiToModel } from "@/api/converters/agentsConverters";
import { mapAgentValidationField } from "@/api/fieldMapping/agentFieldMapping";
import * as api from "@/api/api.generated";
import LoadingTargets from "@/shared/loadingTargets";
import Agent from "@/models/agent";
import RecognizedSource from "@/models/recognizedSource";
import { agentRef } from "@/models/types";
import * as agentsActions from "./actions";
import * as actionsInternal from "./actionsInternal";

/**
 * Fetches all agents from the API and updates the store.
 */
export function* fetchAgentsSaga(): SagaGenerator {
    yield* withLoading(LoadingTargets.Agents, function* () {
        const agents: Agent[] = yield* callApi(() => apiClient().agents.agentsAll())
            .fetchArray(convertAgentApiToModel);
        yield put(actionsInternal.setAgents(agents));
    });
}

/**
 * Fetches the names of the available scanning agent handlers and resolves the caller's callback
 * with them. The list is static per application version, so it is not kept in the store.
 */
export function* fetchAgentHandlersSaga(action: ReturnType<typeof agentsActions.fetchAgentHandlers>): SagaGenerator {
    yield* withCallback(action.meta, function* () {
        const handlers: string[] | null = yield* callApi(() => apiClient().agents.handlers())
            .invoke();
        return handlers ?? [];
    });
}

/**
 * Asks the API which agents can scan the given product URL and resolves the caller's callback with
 * them. A one-off query about a URL the user typed, so there is nothing to keep in the store.
 */
export function* recognizeSourceUrlSaga(action: ReturnType<typeof agentsActions.recognizeSourceUrl>): SagaGenerator {
    yield* withCallback(action.meta, function* () {
        const recognized: RecognizedSource[] = yield* callApi(() => apiClient().agents.recognize({ url: action.payload }))
            .fetchArray(convertRecognizedSourceApiToModel);
        return recognized;
    });
}

/**
 * Saves an agent via the API.
 */
export function* saveAgentSaga(action: ReturnType<typeof agentsActions.saveAgent>): SagaGenerator {
    yield* withLoading(LoadingTargets.AgentEdit, function* () {
        yield* withValidatableCallback(action.meta, {
            mapValidationField: mapAgentValidationField,
        }, function* () {
            const agentToSave = action.payload;
            const isNew = agentToSave.id === agentRef.default();

            if (isNew) {
                const createRequest: api.CreateAgentRequest = {
                    key: agentToSave.key,
                    url: agentToSave.url,
                    pricePattern: agentToSave.pricePattern,
                    handler: agentToSave.handler,
                    decimalDelimiter: agentToSave.decimalDelimiter,
                    urlPattern: agentToSave.urlPattern ?? undefined,
                };
                const result = yield* callApi(() => apiClient().agents.agentsPOST(createRequest))
                    .invoke();
                if (result == null) {
                    throw new Error("API did not return created agent.");
                }
                const created: Agent = {
                    ...agentToSave,
                    id: result.entityId,
                    lastModified: result.lastModified,
                };
                yield put(actionsInternal.setAgent(created));
                return result.entityId;
            } else {
                const updateRequest: api.UpdateAgentRequest = {
                    id: agentToSave.id,
                    lastModified: agentToSave.lastModified,
                    key: agentToSave.key,
                    url: agentToSave.url,
                    pricePattern: agentToSave.pricePattern,
                    handler: agentToSave.handler,
                    decimalDelimiter: agentToSave.decimalDelimiter,
                    urlPattern: agentToSave.urlPattern ?? undefined,
                };
                const result = yield* callApi(() => apiClient().agents.agentsPUT(updateRequest))
                    .invoke();
                if (result == null) {
                    throw new Error("API did not return updated agent.");
                }
                const updated: Agent = {
                    ...agentToSave,
                    lastModified: result.lastModified,
                };
                yield put(actionsInternal.setAgent(updated));
                return result.entityId;
            }
        });
    });
}

/**
 * Deletes an agent via the API.
 */
export function* deleteAgentSaga(action: ReturnType<typeof agentsActions.deleteAgent>): SagaGenerator {
    yield* withLoading(LoadingTargets.Agents, function* () {
        yield* withCallback(action.meta, function* () {
            yield* callApi(() => apiClient().agents.agentsDELETE(action.payload))
                .invoke();
            yield put(actionsInternal.removeAgent(action.payload));
        });
    });
}
