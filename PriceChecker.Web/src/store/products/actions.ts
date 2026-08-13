import { createAction } from "@reduxjs/toolkit";
import { createActionWithMeta, createActionWithMetaValidatable } from "@hwndmaster/atom-react-redux";
import Product from "@/models/product";
import type { ProductSchemaData } from "@/schemas/productSchema";
import { ProductRef } from "@/models/types";

export const fetchProductOverviews = createAction<void>("products/fetchProductOverviews");
/** Resolves with the fetched product; the edit form owns it as its own state. */
export const fetchProduct = createActionWithMeta<ProductRef, Product>("products/fetchProduct");
export const fetchPrices = createAction<ProductRef>("products/fetchPrices");
export const saveProduct = createActionWithMetaValidatable<Product, ProductRef, ProductSchemaData>("products/saveProduct");
export const deleteProduct = createActionWithMeta<ProductRef>("products/deleteProduct");
export const dropPrices = createActionWithMeta<ProductRef>("products/dropPrices");
