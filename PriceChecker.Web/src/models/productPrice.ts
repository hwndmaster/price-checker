import { AgentHandlingStatus } from "./enums";
import { ProductPriceRef, ProductSourceRef } from "./types";

interface ProductPrice {
    id: ProductPriceRef;
    productSourceId: ProductSourceRef;
    status: AgentHandlingStatus;
    price: number | null;
    foundDate: number;
}

export default ProductPrice;
