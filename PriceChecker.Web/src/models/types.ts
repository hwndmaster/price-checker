import { EntityGuidId, createGuidRefConverter } from "@hwndmaster/atom-web-core";

/**
 * A point in time as the API expresses it: .NET `DateTimeOffset` ticks, i.e. 100-nanosecond
 * intervals since Jan 1, 0001. Never read as a plain number — `ticksToDate` from
 * `@hwndmaster/atom-web-core`, or the `formatDate*` helpers in `shared/formatters`, turn it into
 * something displayable.
 */
export type TimeStamp = number;

export type AgentRef = EntityGuidId<"Agent">;
export type ProductRef = EntityGuidId<"Product">;
export type ProductSourceRef = EntityGuidId<"ProductSource">;
export type ProductPriceRef = EntityGuidId<"ProductPrice">;

export const agentRef = createGuidRefConverter<AgentRef>();
export const productRef = createGuidRefConverter<ProductRef>();
export const productSourceRef = createGuidRefConverter<ProductSourceRef>();
export const productPriceRef = createGuidRefConverter<ProductPriceRef>();
