import { SourceUrlMatchKind } from "./enums";
import { AgentRef } from "./types";

/**
 * An agent that recognized a pasted product URL, with the agent argument read off it.
 */
interface RecognizedSource {
    agentId: AgentRef;
    agentKey: string;
    agentArgument: string;
    matchKind: SourceUrlMatchKind;
}

export default RecognizedSource;
