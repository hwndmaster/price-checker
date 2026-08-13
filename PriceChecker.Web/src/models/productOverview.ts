import { ProductScanStatus } from "./enums";
import { ProductRef } from "./types";

/**
 * A product row of the products list, enriched with the outcome of the latest price scans.
 */
interface ProductOverview {
    id: ProductRef;
    name: string;
    category: string | null;
    description: string | null;
    status: ProductScanStatus;
    /** An additional status detail. Only assigned by the live scan updates. */
    statusText: string | null;
    lowestPrice: number | null;
    lowestFoundDate: number | null;
    recentPrice: number | null;
    lastScannedDate: number | null;
    lastModified: number;
}

export default ProductOverview;
