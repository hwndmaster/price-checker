import * as api from "@/api/api.generated";
import Agent from "@/models/agent";

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
        lastModified: apiAgent.lastModified,
    };
}
