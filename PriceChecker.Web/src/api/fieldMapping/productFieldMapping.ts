import type { ProductSchemaData } from "@/schemas/productSchema";
import { normalizeApiFieldName } from "./helpers";

type ProductValidationFieldName = Extract<keyof ProductSchemaData, string>;

const productValidationFieldMap: Partial<Record<string, ProductValidationFieldName>> = {
    Name: "name",
    Category: "category",
    Description: "description",
    Sources: "sources",
};

/**
 * Maps backend validation field names to product form field names.
 */
export function mapProductValidationField(apiFieldName: string): ProductValidationFieldName | undefined {
    return productValidationFieldMap[normalizeApiFieldName(apiFieldName)];
}
