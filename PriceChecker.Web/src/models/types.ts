import { EntityGuidId, createGuidRefConverter } from "@hwndmaster/atom-web-core";

export type AgentRef = EntityGuidId<"Agent">;
export type ProductRef = EntityGuidId<"Product">;
export type ProductSourceRef = EntityGuidId<"ProductSource">;
export type ProductPriceRef = EntityGuidId<"ProductPrice">;

export const agentRef = createGuidRefConverter<AgentRef>();
export const productRef = createGuidRefConverter<ProductRef>();
export const productSourceRef = createGuidRefConverter<ProductSourceRef>();
export const productPriceRef = createGuidRefConverter<ProductPriceRef>();
