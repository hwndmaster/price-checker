import { put } from "redux-saga/effects";
import { callApi, type SagaGenerator, withCallback, withLoading, withValidatableCallback } from "@hwndmaster/atom-react-redux";
import apiClient from "@/api/apiAxios";
import { convertAgentApiToModel } from "@/api/converters/agentsConverters";
import { mapAgentValidationField } from "@/api/fieldMapping/agentFieldMapping";
import * as api from "@/api/api.generated";
import LoadingTargets from "@/shared/loadingTargets";
import Agent from "@/models/agent";
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
