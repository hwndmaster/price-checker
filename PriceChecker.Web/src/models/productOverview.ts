import { ProductScanStatus } from "./enums";
import { ProductRef, TimeStamp } from "./types";

/**
 * One of the pages a product's price is read from, as the list offers it for opening.
 */
interface ProductSourceLink {
    /** The agent the source is scanned with. It names the site when a product has several. */
    agentKey: string;
    url: string;
}

/**
 * A product row of the products list, enriched with the outcome of the latest price scans.
 */
interface ProductOverview {
    id: ProductRef;
    name: string;
    category: string | null;
    description: string | null;
    /** The pages this product is tracked on, ordered by agent key. Empty when it has no sources. */
    sources: ProductSourceLink[];
    status: ProductScanStatus;
    /** Why the last scan did not fully succeed, per failing source; one line each. */
    statusText: string | null;
    lowestPrice: number | null;
    lowestFoundDate: TimeStamp | null;
    recentPrice: number | null;
    /** The price at or below which the product is worth buying, or `null` when it tracks none. */
    targetPrice: number | null;
    lastScannedDate: TimeStamp | null;
    lastModified: TimeStamp;
}

export default ProductOverview;
export type { ProductSourceLink };
