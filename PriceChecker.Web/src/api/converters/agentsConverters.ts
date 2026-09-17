import * as api from "@/api/api.generated";
import Agent from "@/models/agent";
import { SourceUrlMatchKind } from "@/models/enums";
import RecognizedSource from "@/models/recognizedSource";

/**
 * Converts an API AgentDto to an Agent model.
 * @param apiAgent The AgentDto from the API.
 * @returns The converted Agent model.
 */
export function convertAgentApiToModel(apiAgent: api.AgentDto): Agent {
    return {
        id: apiAgent.id,
        key: apiAgent.key,
        url: apiAgent.url,
        pricePattern: apiAgent.pricePattern,
        handler: apiAgent.handler,
        decimalDelimiter: apiAgent.decimalDelimiter,
        urlPattern: apiAgent.urlPattern ?? null,
        lastModified: apiAgent.lastModified,
    };
}

/**
 * Converts an API RecognizedSourceDto to a RecognizedSource model.
 * @param apiRecognized The RecognizedSourceDto from the API.
 * @returns The converted RecognizedSource model.
 */
export function convertRecognizedSourceApiToModel(apiRecognized: api.RecognizedSourceDto): RecognizedSource {
    return {
        agentId: apiRecognized.agentId,
        agentKey: apiRecognized.agentKey,
        agentArgument: apiRecognized.agentArgument,
        matchKind: apiRecognized.matchKind as SourceUrlMatchKind,
    };
}
