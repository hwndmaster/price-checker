import { createSelector } from "@reduxjs/toolkit";
import ProductOverview from "@/models/productOverview";
import ProductPrice from "@/models/productPrice";
import { ProductRef } from "@/models/types";
import AppState from "@/store/appState";

export const selectProductOverviewById: (state: AppState, productId: ProductRef) => ProductOverview | undefined = createSelector(
    [(state: AppState, productId: ProductRef): { overviews: ProductOverview[]; productId: ProductRef } => {
        return { overviews: state.products.overviews, productId };
    }],
    ({ overviews, productId }) => {
        return overviews.find((p) => p.id === productId);
    }
);

export const selectPricesByProductId: (state: AppState, productId: ProductRef) => ProductPrice[] = createSelector(
    [(state: AppState, productId: ProductRef): { prices: Record<string, ProductPrice[]>; productId: ProductRef } => {
        return { prices: state.products.prices, productId };
    }],
    ({ prices, productId }) => {
        return prices[productId] ?? [];
    }
);

export const selectCategories: (state: AppState) => string[] = createSelector(
    [(state: AppState): ProductOverview[] => state.products.overviews],
    (overviews) => {
        return [...new Set(overviews
            .map((p) => p.category)
            .filter((c): c is string => c != null && c.length > 0))]
            .sort((a, b) => a.localeCompare(b));
    }
);
