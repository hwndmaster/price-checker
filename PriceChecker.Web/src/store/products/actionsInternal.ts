import { createAction } from "@reduxjs/toolkit";
import ProductOverview from "@/models/productOverview";
import ProductPrice from "@/models/productPrice";
import { ProductRef } from "@/models/types";

export const setOverviews = createAction<ProductOverview[]>("products/setOverviews");
export const removeProduct = createAction<ProductRef>("products/removeProduct");
export const setPrices = createAction<{ productId: ProductRef; prices: ProductPrice[] }>("products/setPrices");
