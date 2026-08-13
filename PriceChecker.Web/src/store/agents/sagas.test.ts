import { SagaRunner } from "@hwndmaster/atom-testing-utils";
import { Common } from "@hwndmaster/atom-react-redux";
import * as api from "@/api/api.generated";
import LoadingTargets from "@/shared/loadingTargets";
import { fakeAxios } from "@/store/testUtils/sagas";
import { createAgent } from "@/utils/tests/testModels";
import { fetchAgentsSaga } from "./sagas";
import * as actionsInternal from "./actionsInternal";

const sagaRunner = new SagaRunner();

beforeEach(() => {
    fakeAxios.reset();
    sagaRunner.reset();
    vi.clearAllMocks();
});

it("fetchAgentsSaga: fetches and stores all agents", async () => {
    // Arrange
    const agent = createAgent();
    const apiAgent: api.AgentDto = {
        id: agent.id,
        key: agent.key,
        url: agent.url,
        pricePattern: agent.pricePattern,
        handler: agent.handler,
        decimalDelimiter: agent.decimalDelimiter,
        dateCreated: 1,
        lastModified: agent.lastModified,
    };
    fakeAxios.setupGet(api.AgentsClient, "agentsAll").reply(200, [apiAgent]);

    // Act
    await sagaRunner.runSaga(fetchAgentsSaga);

    // Assert
    expect(sagaRunner.findDispatchedAction(Common.Actions.showLoader)).toBe(LoadingTargets.Agents);
    expect(sagaRunner.findDispatchedAction(actionsInternal.setAgents)).toEqual([agent]);
    expect(sagaRunner.findDispatchedAction(Common.Actions.hideLoader)).toBe(LoadingTargets.Agents);
});
