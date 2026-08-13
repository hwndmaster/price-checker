import ProductOverview from "@/models/productOverview";
import ProductPrice from "@/models/productPrice";
import { ProductRef } from "@/models/types";

interface ProductsState {
    overviews: ProductOverview[];
    /** Price history per product id. */
    prices: Record<ProductRef, ProductPrice[]>;
}

export default ProductsState;
