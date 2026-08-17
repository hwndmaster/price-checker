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
import { MobileBreakpoint } from "@/shared/constants";
import LoadingTargets from "@/shared/loadingTargets";
import { formatDateFromTicks, formatPrice } from "@/shared/formatters";
import { getScanStatusPresentation } from "@/shared/scanStatus";
import * as store from "@/store";
import ProductEdit from "@/components/productEdit/productEdit";
import styles from "@/styles/dataTablePage.module.scss";

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
        // On mobile the badge is reduced to its icon, so the tooltip has to carry the label as well.
        const tooltip = product.statusText != null
            ? `${presentation.label}: ${product.statusText}`
            : presentation.label;
        return (
            <>
                <Tooltip target={`#${elementId}`} content={tooltip} event="both" position="left" />
                <Tag
                    id={elementId}
                    icon={presentation.icon}
                    severity={presentation.severity}
                    value={presentation.label}
                    className={styles.statusTag}
                    // Makes the tag focusable, so that tapping it opens the tooltip on a touch device.
                    tabIndex={0}
                    data-test_id="Products__Status"
                />
            </>
        );
    };

    const header = (
        <div className={styles.header}>
            <div className={styles.headerButtons}>
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
            <IconField iconPosition="left" className={styles.searchField}>
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
        <div className={styles.rowActions}>
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
                    <span className={styles.groupHeader} data-test_id="Products__Category_Group">{product.category ?? "Uncategorized"}</span>
                )}
                emptyMessage="No products yet. Add your first product to start tracking prices."
                className={styles.responsiveTable}
                responsiveLayout="stack"
                breakpoint={MobileBreakpoint}
                data-test_id="Products__Table"
            >
                {/* The column widths are set on the header cells only: in the stacked layout the body
                    cells span the whole card and must not be constrained. */}
                <Column body={statusTemplate} header="Status" headerStyle={{ width: "12rem" }} />
                <Column field="name" header="Name" sortable data-test_id="Products__Name" />
                <Column
                    field="lowestPrice"
                    header="Lowest Price"
                    sortable
                    align="right"
                    body={(p: ProductOverview) => formatPrice(p.lowestPrice)}
                    headerStyle={{ width: "10rem" }}
                />
                <Column
                    field="recentPrice"
                    header="Recent Price"
                    sortable
                    align="right"
                    body={(p: ProductOverview) => formatPrice(p.recentPrice)}
                    headerStyle={{ width: "10rem" }}
                />
                <Column
                    field="lowestFoundDate"
                    header="Lowest Found On"
                    sortable
                    body={(p: ProductOverview) => formatDateFromTicks(p.lowestFoundDate)}
                    headerStyle={{ width: "11rem" }}
                />
                <Column
                    field="lastScannedDate"
                    header="Last Updated"
                    sortable
                    body={(p: ProductOverview) => formatDateFromTicks(p.lastScannedDate)}
                    headerStyle={{ width: "11rem" }}
                />
                <Column body={actionsTemplate} headerStyle={{ width: "13rem" }} />
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
