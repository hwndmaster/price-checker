import React, { useEffect, useRef, useState } from "react";
import { LoadingSpinner } from "@hwndmaster/atom-react-redux";
import { toastService } from "@hwndmaster/atom-react-prime";
import {
    Button,
    Column,
    confirmDialog,
    DataTable,
    Dialog,
    IconField,
    InputIcon,
    InputText,
    Tag,
    Tooltip,
} from "@/primereact";
import { ProductScanStatus } from "@/models/enums";
import ProductOverview from "@/models/productOverview";
import { ProductRef, productRef } from "@/models/types";
import LoadingTargets from "@/shared/loadingTargets";
import { formatDateFromTicks, formatPrice } from "@/shared/formatters";
import { getScanStatusPresentation } from "@/shared/scanStatus";
import * as store from "@/store";
import ProductEdit from "@/components/productEdit/productEdit";

const Products: React.FC = () => {
    const dispatch = store.useAppDispatch();
    const overviews = store.useAppSelector((state) => state.products.overviews);
    const isScanRunning = store.useAppSelector((state) => store.Scans.Selectors.selectIsScanRunning(state));

    const [globalFilter, setGlobalFilter] = useState("");
    const [editedProductId, setEditedProductId] = useState<ProductRef | null>(null);
    const [isEditDialogVisible, setEditDialogVisible] = useState(false);

    const hasFetched = useRef(false);
    useEffect(() => {
        if (hasFetched.current) return;
        hasFetched.current = true;
        dispatch(store.Products.Actions.fetchProductOverviews());
        dispatch(store.Scans.Actions.fetchProgress());
    }, [dispatch]);

    const openAddDialog = (): void => {
        setEditedProductId(null);
        setEditDialogVisible(true);
    };

    const openEditDialog = (product: ProductOverview): void => {
        setEditedProductId(product.id);
        setEditDialogVisible(true);
    };

    const deleteProduct = (product: ProductOverview): void => {
        confirmDialog({
            message: `Are you sure you want to delete the '${product.name}' product?`,
            header: "Delete product",
            icon: "pi pi-exclamation-triangle",
            accept: () => {
                dispatch(store.Products.Actions.deleteProduct(product.id, () => {
                    toastService.showSuccess("Product deleted", `The product '${product.name}' has been deleted.`);
                }));
            },
        });
    };

    const dropPrices = (product: ProductOverview): void => {
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

    const statusTemplate = (product: ProductOverview): React.ReactNode => {
        const presentation = getScanStatusPresentation(product.status);
        const elementId = `product-status-${product.id}`;
        return (
            <>
                {product.statusText != null
                    && <Tooltip target={`#${elementId}`} content={product.statusText} />}
                <Tag
                    id={elementId}
                    icon={presentation.icon}
                    severity={presentation.severity}
                    value={presentation.label}
                    data-test_id="Products__Status"
                />
            </>
        );
    };

    const header = (
        <div className="flex flex-wrap align-items-center justify-content-between gap-2">
            <div className="flex gap-2">
                <Button
                    label="Add Product"
                    icon="pi pi-plus"
                    onClick={openAddDialog}
                    data-test_id="Products__Add_Button"
                />
                <Button
                    label="Scan All"
                    icon="pi pi-sync"
                    severity="help"
                    disabled={isScanRunning}
                    onClick={() => dispatch(store.Scans.Actions.scanAllProducts())}
                    data-test_id="Products__Scan_All_Button"
                />
            </div>
            <IconField iconPosition="left">
                <InputIcon className="pi pi-search" />
                <InputText
                    placeholder="Search..."
                    value={globalFilter}
                    onChange={(e) => setGlobalFilter(e.target.value)}
                    data-test_id="Products__Search"
                />
            </IconField>
        </div>
    );

    const actionsTemplate = (product: ProductOverview): React.ReactNode => (
        <div className="flex gap-1">
            <Button
                icon="pi pi-sync"
                rounded text
                severity="help"
                tooltip="Scan prices"
                disabled={product.status === ProductScanStatus.Scanning}
                onClick={() => dispatch(store.Scans.Actions.scanProduct(product.id))}
                data-test_id="Products__Scan_Button"
            />
            <Button
                icon="pi pi-pencil"
                rounded text
                tooltip="Edit"
                onClick={() => openEditDialog(product)}
                data-test_id="Products__Edit_Button"
            />
            <Button
                icon="pi pi-eraser"
                rounded text
                severity="warning"
                tooltip="Drop price history"
                onClick={() => dropPrices(product)}
                data-test_id="Products__Drop_Prices_Button"
            />
            <Button
                icon="pi pi-trash"
                rounded text
                severity="danger"
                tooltip="Delete"
                onClick={() => deleteProduct(product)}
                data-test_id="Products__Delete_Button"
            />
        </div>
    );

    return (
        <LoadingSpinner target={LoadingTargets.Products}>
            <DataTable
                value={overviews}
                dataKey="id"
                header={header}
                globalFilter={globalFilter}
                globalFilterFields={["name", "category", "description"]}
                sortMode="single"
                sortField="name"
                sortOrder={1}
                rowGroupMode="subheader"
                groupRowsBy="category"
                rowGroupHeaderTemplate={(product: ProductOverview) => (
                    <span className="font-bold" data-test_id="Products__Category_Group">{product.category ?? "Uncategorized"}</span>
                )}
                emptyMessage="No products yet. Add your first product to start tracking prices."
                data-test_id="Products__Table"
            >
                <Column body={statusTemplate} header="Status" style={{ width: "12rem" }} />
                <Column field="name" header="Name" sortable data-test_id="Products__Name" />
                <Column
                    field="lowestPrice"
                    header="Lowest Price"
                    sortable
                    body={(p: ProductOverview) => formatPrice(p.lowestPrice)}
                    style={{ textAlign: "right", width: "10rem" }}
                />
                <Column
                    field="recentPrice"
                    header="Recent Price"
                    sortable
                    body={(p: ProductOverview) => formatPrice(p.recentPrice)}
                    style={{ textAlign: "right", width: "10rem" }}
                />
                <Column
                    field="lowestFoundDate"
                    header="Lowest Found On"
                    sortable
                    body={(p: ProductOverview) => formatDateFromTicks(p.lowestFoundDate)}
                    style={{ width: "11rem" }}
                />
                <Column
                    field="lastScannedDate"
                    header="Last Updated"
                    sortable
                    body={(p: ProductOverview) => formatDateFromTicks(p.lastScannedDate)}
                    style={{ width: "11rem" }}
                />
                <Column body={actionsTemplate} style={{ width: "13rem" }} />
            </DataTable>

            <Dialog
                header={editedProductId == null ? "Add Product" : "Edit Product"}
                visible={isEditDialogVisible}
                style={{ width: "42rem" }}
                onHide={() => setEditDialogVisible(false)}
                data-test_id="Products__Edit_Dialog"
            >
                <ProductEdit
                    productId={editedProductId ?? productRef.default()}
                    onClose={() => setEditDialogVisible(false)}
                />
            </Dialog>
        </LoadingSpinner>
    );
};

export default Products;
