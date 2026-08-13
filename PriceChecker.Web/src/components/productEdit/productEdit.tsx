import React, { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useFieldArray, useForm } from "react-hook-form";
import { translateErrorsToForm } from "@hwndmaster/atom-react-core";
import { FormDropdown, FormInputText, FormInputTextarea, toastService } from "@hwndmaster/atom-react-prime";
import { LoadingSpinner } from "@hwndmaster/atom-react-redux";
import { Button } from "@/primereact";
import { ProductRef, productRef, agentRef } from "@/models/types";
import Product, { ProductSource } from "@/models/product";
import { productSchema, ProductSchemaData } from "@/schemas/productSchema";
import * as store from "@/store";
import LoadingTargets from "@/shared/loadingTargets";
import styles from "./productEdit.module.scss";

interface ProductEditProps {
    /** The product to edit, or `productRef.default()` in the add mode. */
    productId: ProductRef;
    onClose: () => void;
}

const ProductEdit: React.FC<ProductEditProps> = ({ productId, onClose }) => {
    const dispatch = store.useAppDispatch();
    const isAddMode = productId === productRef.default();
    const agents = store.useAppSelector((state) => state.agents.agents);
    const [product, setProduct] = useState<Product | null>(null);

    const form = useForm<ProductSchemaData>({
        resolver: zodResolver(productSchema),
        defaultValues: {
            name: "",
            category: null,
            description: null,
            sources: [],
        }
    });
    const sourcesField = useFieldArray({ control: form.control, name: "sources" });

    useEffect(() => {
        if (agents.length === 0) {
            dispatch(store.Agents.Actions.fetchAgents());
        }
        if (!isAddMode) {
            // The resolve callback is typed as possibly undefined, whereas "no product" is null here.
            dispatch(store.Products.Actions.fetchProduct(productId, (fetched) => setProduct(fetched ?? null)));
        }
    }, [dispatch, isAddMode, productId, agents.length]);

    useEffect(() => {
        if (!isAddMode && product != null) {
            form.reset({
                name: product.name,
                category: product.category,
                description: product.description,
                sources: product.sources.map((s) => ({
                    agentId: s.agentId,
                    agentArgument: s.agentArgument,
                })),
            });
        }
    }, [product, form, isAddMode]);

    const onSubmit = (data: ProductSchemaData): void => {
        // Preserve the existing source ids by their position, so that the price history
        // of the unchanged sources survives the update.
        const sources: ProductSource[] = data.sources.map((s, index) => ({
            id: product?.sources[index]?.id ?? null,
            agentId: s.agentId,
            agentArgument: s.agentArgument,
        }));
        const productData: Product = {
            id: product?.id ?? productRef.default(),
            lastModified: product?.lastModified ?? 0,
            name: data.name,
            category: data.category != null && data.category.length > 0 ? data.category : null,
            description: data.description != null && data.description.length > 0 ? data.description : null,
            sources,
        };

        form.clearErrors();

        dispatch(store.Products.Actions.saveProduct(productData, translateErrorsToForm(form), (savedProductId: ProductRef | undefined) => {
            if (savedProductId == null) {
                toastService.showError("Save failed", "An error occurred while saving the product.");
                return;
            }

            toastService.showSuccess(
                isAddMode ? "Product added" : "Product updated",
                "The product has been successfully " + (isAddMode ? "added." : "updated."));
            onClose();
        }));
    };

    if (!isAddMode && product == null) {
        return (
            <LoadingSpinner target={LoadingTargets.ProductEdit}>
                <div>Loading product...</div>
            </LoadingSpinner>
        );
    }

    return (
        <LoadingSpinner target={LoadingTargets.ProductEdit}>
            <form onSubmit={(e) => void form.handleSubmit(onSubmit)(e)} className={styles.form}>
                <div className={`${styles.row} ${styles.firstRow}`}>
                    <FormInputText name="name" form={form} label="Name" data-test_id="ProductEdit__Name_Input" />
                </div>
                <div className={styles.row}>
                    <FormInputText name="category" form={form} label="Category" data-test_id="ProductEdit__Category_Input" />
                </div>
                <div className={styles.row}>
                    <FormInputTextarea name="description" form={form} label="Description" data-test_id="ProductEdit__Description_Input" />
                </div>

                <h4 className={styles.sourcesHeader}>Sources</h4>
                {sourcesField.fields.map((field, index) => (
                    <div key={field.id} className={`${styles.row} ${styles.sourceRow}`}>
                        <FormDropdown
                            name={`sources.${index}.agentId`}
                            form={form}
                            label="Agent"
                            options={agents}
                            optionLabel="key"
                            optionValue="id"
                            data-test_id="ProductEdit__Source_Agent_Dropdown"
                        />
                        <FormInputText
                            name={`sources.${index}.agentArgument`}
                            form={form}
                            label="Argument"
                            className={styles.sourceArgument}
                            data-test_id="ProductEdit__Source_Argument_Input"
                        />
                        <Button
                            type="button"
                            icon="pi pi-trash"
                            rounded text
                            severity="danger"
                            onClick={() => sourcesField.remove(index)}
                            data-test_id="ProductEdit__Source_Delete_Button"
                        />
                    </div>
                ))}
                <div className={styles.row}>
                    <Button
                        type="button"
                        label="Add Source"
                        icon="pi pi-plus"
                        severity="secondary"
                        outlined
                        onClick={() => sourcesField.append({ agentId: agentRef.default(), agentArgument: "" })}
                        data-test_id="ProductEdit__Add_Source_Button"
                    />
                </div>

                <div className={styles.actions}>
                    <Button type="submit" label="Save" icon="pi pi-check" data-test_id="ProductEdit__Save_Button" />
                    <Button type="button" label="Cancel" icon="pi pi-times" severity="secondary" onClick={onClose} data-test_id="ProductEdit__Cancel_Button" />
                </div>
            </form>
        </LoadingSpinner>
    );
};

export default ProductEdit;
