import { z } from "zod";

export const agentSchema = z.object({
    key: z.string().min(1, "Key is required"),
    url: z.string().min(1, "URL is required"),
    pricePattern: z.string().min(1, "Price pattern is required"),
    handler: z.string().min(1, "Handler is required"),
    decimalDelimiter: z.string().length(1, "The decimal delimiter must be a single character"),
});

export type AgentSchemaData = z.infer<typeof agentSchema>;
