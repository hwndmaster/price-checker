import { AgentRef, ProductRef, ProductSourceRef } from "./types";

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
    sources: ProductSource[];
    lastModified: number;
}

export default Product;
export type { ProductSource };
