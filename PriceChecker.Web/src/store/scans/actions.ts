import { createAction } from "@reduxjs/toolkit";
import ProductOverview from "@/models/productOverview";
import ScanProgress from "@/models/scanProgress";
import { ProductRef } from "@/models/types";

export const scanAllProducts = createAction<void>("scans/scanAllProducts");
export const scanProduct = createAction<ProductRef>("scans/scanProduct");
export const fetchProgress = createAction<void>("scans/fetchProgress");

// The following actions are dispatched by the scan hub subscription (see messages.ts):
export const productScanStarted = createAction<ProductRef>("scans/productScanStarted");
export const productScanFailed = createAction<{ productId: ProductRef; errorMessage: string }>("scans/productScanFailed");
export const productScanFinished = createAction<ProductOverview>("scans/productScanFinished");
export const progressChanged = createAction<ScanProgress>("scans/progressChanged");
