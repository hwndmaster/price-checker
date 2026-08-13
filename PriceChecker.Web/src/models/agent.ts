import { AgentRef } from "./types";

interface Agent {
    id: AgentRef;
    key: string;
    url: string;
    pricePattern: string;
    handler: string;
    decimalDelimiter: string;
    lastModified: number;
}

export default Agent;
