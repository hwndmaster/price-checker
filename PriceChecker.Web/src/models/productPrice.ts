import { AgentHandlingStatus } from "./enums";
import { ProductPriceRef, ProductSourceRef, TimeStamp } from "./types";

interface ProductPrice {
    id: ProductPriceRef;
    productSourceId: ProductSourceRef;
    status: AgentHandlingStatus;
    price: number | null;
    foundDate: TimeStamp;
}

export default ProductPrice;
