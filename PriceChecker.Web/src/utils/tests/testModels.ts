import type Agent from "@/models/agent";
import { AgentHandlingStatus, ProductScanStatus } from "@/models/enums";
import type Product from "@/models/product";
import type ProductOverview from "@/models/productOverview";
import type ProductPrice from "@/models/productPrice";
import type ScanProgress from "@/models/scanProgress";
import { agentRef, productPriceRef, productRef, productSourceRef } from "@/models/types";

let idCounter = 0;

function nextGuid(): string {
    idCounter += 1;
    return `00000000-0000-0000-0000-${idCounter.toString().padStart(12, "0")}`;
}

/** Resets the shared test model id counter back to zero. */
export function resetTestModelIdCounter(): void {
    idCounter = 0;
}

/** Creates an Agent model with generated id and override support. */
export function createAgent(overrides?: Partial<Agent>): Agent {
    return {
        id: agentRef(nextGuid()),
        key: "test-agent",
        url: "https://example.com/{0}",
        pricePattern: "`(?<price>[\\d.]+)`",
        handler: "SimpleRegex",
        decimalDelimiter: ".",
        lastModified: 1,
        ...overrides,
    };
}

/** Creates a Product model with generated id and override support. */
export function createProduct(overrides?: Partial<Product>): Product {
    return {
        id: productRef(nextGuid()),
        name: "Test Product",
        category: "Test Category",
        description: null,
        sources: [],
        lastModified: 1,
        ...overrides,
    };
}

/** Creates a ProductOverview model with generated id and override support. */
export function createProductOverview(overrides?: Partial<ProductOverview>): ProductOverview {
    return {
        id: productRef(nextGuid()),
        name: "Test Product",
        category: "Test Category",
        description: null,
        status: ProductScanStatus.NotScanned,
        statusText: null,
        lowestPrice: null,
        lowestFoundDate: null,
        recentPrice: null,
        lastScannedDate: null,
        lastModified: 1,
        ...overrides,
    };
}

/** Creates a ProductPrice model with generated id and override support. */
export function createProductPrice(overrides?: Partial<ProductPrice>): ProductPrice {
    return {
        id: productPriceRef(nextGuid()),
        productSourceId: productSourceRef(nextGuid()),
        status: AgentHandlingStatus.Success,
        price: 100,
        foundDate: 1,
        ...overrides,
    };
}

/** Creates a ScanProgress model with override support. */
export function createScanProgress(overrides?: Partial<ScanProgress>): ScanProgress {
    return {
        finished: 0,
        total: 0,
        hasErrors: false,
        hasNewLowestPrice: false,
        isFinished: true,
        ...overrides,
    };
}
