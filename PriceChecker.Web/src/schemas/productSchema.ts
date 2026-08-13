import { z } from "zod";
import { requiredGuidRef } from "@hwndmaster/atom-react-core";
import { AgentRef } from "@/models/types";

export const productSourceSchema = z.object({
    agentId: requiredGuidRef<AgentRef>("Agent is required"),
    agentArgument: z.string().min(1, "Argument is required"),
});

export const productSchema = z.object({
    name: z.string().min(1, "Name is required"),
    category: z.string().nullable(),
    description: z.string().nullable(),
    sources: z.array(productSourceSchema),
});

export type ProductSourceSchemaData = z.infer<typeof productSourceSchema>;
export type ProductSchemaData = z.infer<typeof productSchema>;
