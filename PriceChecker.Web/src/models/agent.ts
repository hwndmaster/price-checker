import { AgentRef } from "./types";

interface Agent {
    id: AgentRef;
    key: string;
    url: string;
    pricePattern: string;
    handler: string;
    decimalDelimiter: string;
    /** A regular expression matching the product URLs of the agent's site, or null when it has none. */
    urlPattern: string | null;
    lastModified: number;
}

export default Agent;
