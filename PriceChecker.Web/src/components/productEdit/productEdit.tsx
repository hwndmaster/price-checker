import React, { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useFieldArray, useForm } from "react-hook-form";
import { translateErrorsToForm } from "@hwndmaster/atom-react-core";
import { FormDropdown, FormInputText, FormInputTextarea, toastService } from "@hwndmaster/atom-react-prime";
import { LoadingSpinner } from "@hwndmaster/atom-react-redux";
import { Button, confirmDialog, InputText } from "@/primereact";
import { ProductRef, productRef, agentRef } from "@/models/types";
import Product, { ProductSource } from "@/models/product";
import RecognizedSource from "@/models/recognizedSource";
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
    const [sourceUrl, setSourceUrl] = useState("");
    const [isRecognizing, setIsRecognizing] = useState(false);

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
        // Refetched rather than taken from the persisted list: an agent recognized from a pasted URL
        // has to be in it, and the source rows label their dropdown from it.
        dispatch(store.Agents.Actions.fetchAgents());
        if (!isAddMode) {
            // The resolve callback is typed as possibly undefined, whereas "no product" is null here.
            dispatch(store.Products.Actions.fetchProduct(productId, (fetched) => setProduct(fetched ?? null)));
        }
    }, [dispatch, isAddMode, productId]);

    useEffect(() => {
        if (!isAddMode && product != null) {
            form.reset({
                name: product.name,
                category: product.category,
                description: product.description,
                sources: product.sources.map((s) => ({
                    id: s.id,
                    agentId: s.agentId,
                    agentArgument: s.agentArgument,
                })),
            });
        }
    }, [product, form, isAddMode]);

    /**
     * Turns a pasted product URL into a source: the API says which agent scans that site and what
     * the agent argument is, so that neither has to be worked out by hand.
     */
    const addSourceFromUrl = (): void => {
        const url = sourceUrl.trim();
        if (url.length === 0) {
            return;
        }

        setIsRecognizing(true);
        dispatch(store.Agents.Actions.recognizeSourceUrl(url,
            (matches?: RecognizedSource[]) => {
                setIsRecognizing(false);
                if (matches == null || matches.length === 0) {
                    toastService.showError("Not recognized",
                        "No agent can scan this URL. Add an agent for the site, or fill the source in by hand.");
                    return;
                }

                // The first match is the most specific one; the agent stays editable either way.
                const [match] = matches;
                const isAlreadyAdded = sourcesField.fields.some((_, index) =>
                    form.getValues(`sources.${index}.agentId`) === match.agentId
                    && form.getValues(`sources.${index}.agentArgument`) === match.agentArgument);
                if (isAlreadyAdded) {
                    toastService.showWarn("Already added", `'${match.agentKey}' already scans this product page.`);
                    return;
                }

                sourcesField.append({ id: null, agentId: match.agentId, agentArgument: match.agentArgument });
                setSourceUrl("");

                if (matches.length > 1) {
                    toastService.showInfo("Source added",
                        `${matches.length} agents can scan this URL; '${match.agentKey}' was picked. Change it below if you meant another.`);
                } else {
                    toastService.showSuccess("Source added", `Recognized as '${match.agentKey}'.`);
                }
            },
            () => {
                setIsRecognizing(false);
                toastService.showError("Not recognized", "An error occurred while recognizing the URL.");
            }));
    };

    const dropPrices = (): void => {
        if (product == null) {
            return;
        }

        confirmDialog({
            message: `Are you sure you want to drop the price history of '${product.name}'?`,
            header: "Prices drop confirmation",
            icon: "pi pi-exclamation-triangle",
            accept: () => {
                dispatch(store.Products.Actions.dropPrices(product.id, () => {
                    toastService.showSuccess("Prices dropped", `The price history of '${product.name}' has been dropped.`);
                }));
            },
        });
    };

    const onSubmit = (data: ProductSchemaData): void => {
        // Each row carries the id of the source it was loaded from, so that removing or reordering
        // the rows cannot attach one source's price history to another.
        const sources: ProductSource[] = data.sources.map((s) => ({
            id: s.id,
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
                <div className={`${styles.row} ${styles.sourceRow}`}>
                    <InputText
                        value={sourceUrl}
                        onChange={(e) => setSourceUrl(e.target.value)}
                        onKeyDown={(e) => {
                            if (e.key === "Enter") {
                                // The form would otherwise be submitted by the Enter of a single-line input.
                                e.preventDefault();
                                addSourceFromUrl();
                            }
                        }}
                        placeholder="Paste a product URL to add a source"
                        className={styles.sourceUrl}
                        data-test_id="ProductEdit__Source_Url_Input"
                    />
                    <Button
                        type="button"
                        label="Add from URL"
                        icon="pi pi-link"
                        loading={isRecognizing}
                        disabled={sourceUrl.trim().length === 0}
                        onClick={addSourceFromUrl}
                        data-test_id="ProductEdit__Add_Source_From_Url_Button"
                    />
                </div>
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
                        onClick={() => sourcesField.append({ id: null, agentId: agentRef.default(), agentArgument: "" })}
                        data-test_id="ProductEdit__Add_Source_Button"
                    />
                </div>

                <div className={styles.actions}>
                    {/* Destructive and unrelated to the form, hence kept away from Save/Cancel. */}
                    {!isAddMode && (
                        <Button
                            type="button"
                            label="Drop price history"
                            icon="pi pi-eraser"
                            severity="warning"
                            outlined
                            className={styles.dropPrices}
                            onClick={dropPrices}
                            data-test_id="ProductEdit__Drop_Prices_Button"
                        />
                    )}
                    <Button type="submit" label="Save" icon="pi pi-check" data-test_id="ProductEdit__Save_Button" />
                    <Button type="button" label="Cancel" icon="pi pi-times" severity="secondary" onClick={onClose} data-test_id="ProductEdit__Cancel_Button" />
                </div>
            </form>
        </LoadingSpinner>
    );
};

export default ProductEdit;
