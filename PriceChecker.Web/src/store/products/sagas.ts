import { put } from "redux-saga/effects";
import { callApi, type SagaGenerator, withCallback, withLoading, withValidatableCallback } from "@hwndmaster/atom-react-redux";
import apiClient from "@/api/apiAxios";
import { convertProductApiToModel, convertProductOverviewApiToModel, convertProductPriceApiToModel } from "@/api/converters/productsConverters";
import { mapProductValidationField } from "@/api/fieldMapping/productFieldMapping";
import * as api from "@/api/api.generated";
import LoadingTargets from "@/shared/loadingTargets";
import Product from "@/models/product";
import ProductOverview from "@/models/productOverview";
import ProductPrice from "@/models/productPrice";
import { productRef } from "@/models/types";
import * as productsActions from "./actions";
import * as actionsInternal from "./actionsInternal";

/**
 * Fetches the product overviews from the API and updates the store.
 */
export function* fetchProductOverviewsSaga(): SagaGenerator {
    yield* withLoading(LoadingTargets.Products, function* () {
        const overviews: ProductOverview[] = yield* callApi(() => apiClient().products.overview())
            .fetchArray(convertProductOverviewApiToModel);
        yield put(actionsInternal.setOverviews(overviews));
    });
}

/**
 * Fetches a single product with its sources for editing, and resolves the caller's callback with it.
 * The product is not kept in the store: only the edit form needs it, for as long as it is open.
 */
export function* fetchProductSaga(action: ReturnType<typeof productsActions.fetchProduct>): SagaGenerator {
    yield* withLoading(LoadingTargets.ProductEdit, function* () {
        yield* withCallback(action.meta, function* () {
            const product: Product | null = yield* callApi(() => apiClient().products.productsGET(action.payload))
                .fetch(convertProductApiToModel);
            if (product == null) {
                throw new Error("The product was not found.");
            }
            return product;
        });
    });
}

/**
 * Fetches the price history of a product.
 */
export function* fetchPricesSaga(action: ReturnType<typeof productsActions.fetchPrices>): SagaGenerator {
    yield* withLoading(LoadingTargets.ProductPrices, function* () {
        const prices: ProductPrice[] = yield* callApi(() => apiClient().products.prices(action.payload))
            .fetchArray(convertProductPriceApiToModel);
        yield put(actionsInternal.setPrices({ productId: action.payload, prices }));
    }, action.payload);
}

/**
 * Saves a product via the API.
 */
export function* saveProductSaga(action: ReturnType<typeof productsActions.saveProduct>): SagaGenerator {
    yield* withLoading(LoadingTargets.ProductEdit, function* () {
        yield* withValidatableCallback(action.meta, {
            mapValidationField: mapProductValidationField,
        }, function* () {
            const productToSave = action.payload;
            const isNew = productToSave.id === productRef.default();

            if (isNew) {
                const createRequest: api.CreateProductRequest = {
                    name: productToSave.name,
                    category: productToSave.category ?? undefined,
                    description: productToSave.description ?? undefined,
                    sources: productToSave.sources.map((s) => ({
                        agentId: s.agentId,
                        agentArgument: s.agentArgument,
                    })),
                };
                const result = yield* callApi(() => apiClient().products.productsPOST(createRequest))
                    .invoke();
                if (result == null) {
                    throw new Error("API did not return created product.");
                }
                yield put(productsActions.fetchProductOverviews());
                return result.entityId;
            } else {
                const updateRequest: api.UpdateProductRequest = {
                    id: productToSave.id,
                    lastModified: productToSave.lastModified,
                    name: productToSave.name,
                    category: productToSave.category ?? undefined,
                    description: productToSave.description ?? undefined,
                    sources: productToSave.sources.map((s) => ({
                        id: s.id ?? undefined,
                        agentId: s.agentId,
                        agentArgument: s.agentArgument,
                    })),
                };
                const result = yield* callApi(() => apiClient().products.productsPUT(updateRequest))
                    .invoke();
                if (result == null) {
                    throw new Error("API did not return updated product.");
                }
                yield put(productsActions.fetchProductOverviews());
                return result.entityId;
            }
        });
    });
}

/**
 * Deletes a product via the API.
 */
export function* deleteProductSaga(action: ReturnType<typeof productsActions.deleteProduct>): SagaGenerator {
    yield* withLoading(LoadingTargets.Products, function* () {
        yield* withCallback(action.meta, function* () {
            yield* callApi(() => apiClient().products.productsDELETE(action.payload))
                .invoke();
            yield put(actionsInternal.removeProduct(action.payload));
        });
    });
}

/**
 * Drops the whole price history of a product via the API.
 */
export function* dropPricesSaga(action: ReturnType<typeof productsActions.dropPrices>): SagaGenerator {
    yield* withLoading(LoadingTargets.Products, function* () {
        yield* withCallback(action.meta, function* () {
            yield* callApi(() => apiClient().products.dropPrices(action.payload))
                .invoke();
            yield put(productsActions.fetchProductOverviews());
        });
    });
}
