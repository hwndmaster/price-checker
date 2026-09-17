import { SagaRunner } from "@hwndmaster/atom-testing-utils";
import { Common } from "@hwndmaster/atom-react-redux";
import * as api from "@/api/api.generated";
import LoadingTargets from "@/shared/loadingTargets";
import { fakeAxios } from "@/store/testUtils/sagas";
import { createAgent } from "@/utils/tests/testModels";
import { agentRef } from "@/models/types";
import { SourceUrlMatchKind } from "@/models/enums";
import { fetchAgentHandlersSaga, fetchAgentsSaga, recognizeSourceUrlSaga } from "./sagas";
import * as agentsActions from "./actions";
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
        urlPattern: undefined,
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

it("fetchAgentHandlersSaga: resolves with the handler names", async () => {
    // Arrange
    fakeAxios.setupGet(api.AgentsClient, "handlers").reply(200, ["SimpleRegex", "SimpleRegexDivideBy100"]);
    const resolve = vi.fn();

    // Act
    await sagaRunner.runSaga(fetchAgentHandlersSaga, agentsActions.fetchAgentHandlers(undefined, resolve));

    // Assert
    expect(resolve).toHaveBeenCalledWith(["SimpleRegex", "SimpleRegexDivideBy100"]);
});

it("recognizeSourceUrlSaga: resolves with the recognized agents", async () => {
    // Arrange
    const url = "https://www.amazon.de/Koch-Chemie/dp/B0047AKFDG/ref=sr_1_3";
    const agentId = agentRef("00000000-0000-0000-0000-00000000000a");
    const recognized: api.RecognizedSourceDto = {
        agentId,
        agentKey: "amazon.de",
        agentArgument: "B0047AKFDG",
        matchKind: 0,
    };
    fakeAxios.setupPost(api.AgentsClient, "recognize", { body: { url } }).reply(200, [recognized]);
    const resolve = vi.fn();

    // Act
    await sagaRunner.runSaga(recognizeSourceUrlSaga, agentsActions.recognizeSourceUrl(url, resolve));

    // Assert
    expect(resolve).toHaveBeenCalledWith([{
        agentId,
        agentKey: "amazon.de",
        agentArgument: "B0047AKFDG",
        matchKind: SourceUrlMatchKind.UrlPattern,
    }]);
});

it("recognizeSourceUrlSaga: resolves with nothing when no agent recognizes the URL", async () => {
    // Arrange
    const url = "https://www.mediamarkt.nl/nl/product/_unknown-1.html";
    fakeAxios.setupPost(api.AgentsClient, "recognize", { body: { url } }).reply(200, []);
    const resolve = vi.fn();

    // Act
    await sagaRunner.runSaga(recognizeSourceUrlSaga, agentsActions.recognizeSourceUrl(url, resolve));

    // Assert
    expect(resolve).toHaveBeenCalledWith([]);
});
