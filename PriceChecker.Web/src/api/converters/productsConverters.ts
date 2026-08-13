import * as api from "@/api/api.generated";
import { AgentHandlingStatus, ProductScanStatus } from "@/models/enums";
import Product from "@/models/product";
import ProductOverview from "@/models/productOverview";
import ProductPrice from "@/models/productPrice";

/**
 * Converts an API ProductDto to a Product model.
 * @param apiProduct The ProductDto from the API.
 * @returns The converted Product model.
 */
export function convertProductApiToModel(apiProduct: api.ProductDto): Product {
    return {
        id: apiProduct.id,
        name: apiProduct.name,
        category: apiProduct.category ?? null,
        description: apiProduct.description ?? null,
        sources: apiProduct.sources.map((s) => ({
            id: s.id,
            agentId: s.agentId,
            agentArgument: s.agentArgument,
        })),
        lastModified: apiProduct.lastModified,
    };
}

/**
 * Converts an API ProductOverviewDto to a ProductOverview model.
 * @param apiOverview The ProductOverviewDto from the API.
 * @returns The converted ProductOverview model.
 */
export function convertProductOverviewApiToModel(apiOverview: api.ProductOverviewDto): ProductOverview {
    return {
        id: apiOverview.id,
        name: apiOverview.name,
        category: apiOverview.category ?? null,
        description: apiOverview.description ?? null,
        status: apiOverview.status as ProductScanStatus,
        statusText: null,
        lowestPrice: apiOverview.lowestPrice ?? null,
        lowestFoundDate: apiOverview.lowestFoundDate ?? null,
        recentPrice: apiOverview.recentPrice ?? null,
        lastScannedDate: apiOverview.lastScannedDate ?? null,
        lastModified: apiOverview.lastModified,
    };
}

/**
 * Converts an API ProductPriceDto to a ProductPrice model.
 * @param apiPrice The ProductPriceDto from the API.
 * @returns The converted ProductPrice model.
 */
export function convertProductPriceApiToModel(apiPrice: api.ProductPriceDto): ProductPrice {
    return {
        id: apiPrice.id,
        productSourceId: apiPrice.productSourceId,
        status: apiPrice.status as AgentHandlingStatus,
        price: apiPrice.price ?? null,
        foundDate: apiPrice.foundDate,
    };
}
