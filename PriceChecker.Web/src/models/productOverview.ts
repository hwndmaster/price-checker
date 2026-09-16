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
    /** Why the last scan did not fully succeed, per failing source; one line each. */
    statusText: string | null;
    lowestPrice: number | null;
    lowestFoundDate: number | null;
    recentPrice: number | null;
    lastScannedDate: number | null;
    lastModified: number;
}

export default ProductOverview;
