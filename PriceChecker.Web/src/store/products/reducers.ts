import { createReducer } from "@reduxjs/toolkit";
import { ProductScanStatus } from "@/models/enums";
import * as scansActions from "@/store/scans/actions";
import * as actions from "./actionsInternal";
import ProductsState from "./state";

const initialState: ProductsState = {
    overviews: [],
    prices: {},
};

const productsReducer = createReducer(initialState, (builder) => {
    builder
        .addCase(actions.setOverviews, (state, action) => {
            state.overviews = action.payload;
        })
        .addCase(actions.removeProduct, (state, action) => {
            state.overviews = state.overviews.filter((p) => p.id !== action.payload);
        })
        .addCase(actions.setPrices, (state, action) => {
            state.prices[action.payload.productId] = action.payload.prices;
        })
        // Live scan updates, pushed over the scan hub:
        .addCase(scansActions.productScanStarted, (state, action) => {
            const overview = state.overviews.find((p) => p.id === action.payload);
            if (overview != null) {
                overview.status = ProductScanStatus.Scanning;
                overview.statusText = null;
            }
        })
        .addCase(scansActions.productScanFailed, (state, action) => {
            const overview = state.overviews.find((p) => p.id === action.payload.productId);
            if (overview != null) {
                overview.status = ProductScanStatus.Failed;
                overview.statusText = action.payload.errorMessage;
            }
        })
        .addCase(scansActions.productScanFinished, (state, action) => {
            const index = state.overviews.findIndex((p) => p.id === action.payload.id);
            if (index >= 0) {
                state.overviews[index] = action.payload;
            } else {
                state.overviews.push(action.payload);
            }
        });
});

export default productsReducer;
