import { z } from "zod";
import { requiredGuidRef } from "@hwndmaster/atom-react-core";
import { AgentRef, ProductSourceRef } from "@/models/types";

export const productSourceSchema = z.object({
    /** The source this row was loaded from, or null for a row the user has just added. */
    id: z.custom<ProductSourceRef>().nullable(),
    agentId: requiredGuidRef<AgentRef>("Agent is required"),
    agentArgument: z.string().min(1, "Argument is required"),
});

export const productSchema = z.object({
    name: z.string().min(1, "Name is required"),
    category: z.string().nullable(),
    description: z.string().nullable(),
    /**
     * Whether the product tracks a target price. It is the form's own field: what is stored is the
     * target price alone, and switching the tracking off is what clears it.
     */
    targetPriceEnabled: z.boolean(),
    targetPrice: z.number().nullable(),
    sources: z.array(productSourceSchema),
}).superRefine((data, ctx) => {
    if (!data.targetPriceEnabled) {
        return;
    }

    if (data.targetPrice == null) {
        ctx.addIssue({
            code: "custom",
            path: ["targetPrice"],
            message: "Target price is required",
        });
    } else if (data.targetPrice <= 0) {
        ctx.addIssue({
            code: "custom",
            path: ["targetPrice"],
            message: "Target price must be greater than zero",
        });
    }
});

export type ProductSourceSchemaData = z.infer<typeof productSourceSchema>;
export type ProductSchemaData = z.infer<typeof productSchema>;
