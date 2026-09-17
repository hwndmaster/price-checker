import { AgentRef, ProductRef, ProductSourceRef, TimeStamp } from "./types";

interface ProductSource {
    id: ProductSourceRef | null;
    agentId: AgentRef;
    agentArgument: string;
}

interface Product {
    id: ProductRef;
    name: string;
    category: string | null;
    description: string | null;
    /**
     * The price at or below which the product is worth buying, or `null` when it tracks none.
     * A product that tracks one is reported when its price reaches it, rather than when it beats its
     * own lowest price.
     */
    targetPrice: number | null;
    sources: ProductSource[];
    lastModified: TimeStamp;
}

export default Product;
export type { ProductSource };
