import type { AgentSchemaData } from "@/schemas/agentSchema";
import { normalizeApiFieldName } from "./helpers";

type AgentValidationFieldName = Extract<keyof AgentSchemaData, string>;

const agentValidationFieldMap: Partial<Record<string, AgentValidationFieldName>> = {
    Key: "key",
    Url: "url",
    PricePattern: "pricePattern",
    Handler: "handler",
    DecimalDelimiter: "decimalDelimiter",
};

/**
 * Maps backend validation field names to agent form field names.
 */
export function mapAgentValidationField(apiFieldName: string): AgentValidationFieldName | undefined {
    return agentValidationFieldMap[normalizeApiFieldName(apiFieldName)];
}
