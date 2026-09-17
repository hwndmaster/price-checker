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
    Menu,
    Tag,
    Tooltip,
} from "@/primereact";
import type { DataTableSortMeta, MenuItem } from "@/primereact";
import { ProductScanStatus } from "@/models/enums";
import ProductOverview from "@/models/productOverview";
import { ProductRef, productRef } from "@/models/types";
import { MobileBreakpoint, RowActionTooltipOptions } from "@/shared/constants";
import LoadingTargets from "@/shared/loadingTargets";
import { formatDateFromTicks, formatPrice } from "@/shared/formatters";
import { getScanStatusPresentation } from "@/shared/scanStatus";
import * as store from "@/store";
import ProductEdit from "@/components/productEdit/productEdit";
import styles from "@/styles/dataTablePage.module.scss";

// The category leads the sort because PrimeReact only emits one subheader per *consecutive* run of
// rows sharing the grouping field: sorting by anything else first tears a category into several
// groups. It stays in front when the user sorts by another column, which then orders the products
// within their category.
const productsSortOrder: DataTableSortMeta[] = [
    { field: "category", order: 1 },
    { field: "name", order: 1 },
];

// A value the product has no data for yet renders as an empty string. In the table that is simply an
// empty cell, but on a card it would be a line stating nothing, so the cell is marked for the card
// layout to drop.
const valueCellClass = (formatted: string): string => (formatted === "" ? styles.cardEmptyValue : "");

const Products: React.FC = () => {
    const dispatch = store.useAppDispatch();
    const overviews = store.useAppSelector((state) => state.products.overviews);
    const isScanRunning = store.useAppSelector((state) => store.Scans.Selectors.selectIsScanRunning(state));

    const [globalFilter, setGlobalFilter] = useState("");
    const [editedProductId, setEditedProductId] = useState<ProductRef | null>(null);
    const [isEditDialogVisible, setEditDialogVisible] = useState(false);

    // One menu for the whole table rather than one per row: only ever one is open, and a popup menu
    // per record would mount an overlay for every product in the list.
    const sourcesMenuRef = useRef<Menu>(null);
    const [sourcesMenuItems, setSourcesMenuItems] = useState<MenuItem[]>([]);

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

    // Opened straight from the click handler, never after an await: a browser only honours window.open
    // while it is still handling the gesture that led to it, and blocks it once anything has awaited.
    const openSource = (url: string): void => {
        window.open(url, "_blank", "noopener,noreferrer");
    };

    const openSourcesMenu = (product: ProductOverview, event: React.MouseEvent<HTMLElement>): void => {
        setSourcesMenuItems(product.sources.map((source) => ({
            label: source.agentKey,
            icon: "pi pi-external-link",
            command: (): void => openSource(source.url),
        })));
        sourcesMenuRef.current?.toggle(event);
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

    // The name leads to the pages the product is tracked on: straight there when there is only one,
    // through a menu of the sites when there are several. A product without sources is not yet tracked
    // anywhere, so its name stays plain text rather than offering a click that could do nothing.
    const nameTemplate = (product: ProductOverview): React.ReactNode => {
        if (product.sources.length === 0) {
            return product.name;
        }

        const isSingle = product.sources.length === 1;
        return (
            <button
                type="button"
                className={styles.linkedName}
                title={isSingle
                    ? `Open on ${product.sources[0].agentKey}`
                    : `Open on one of ${product.sources.length} sites`}
                aria-haspopup={isSingle ? undefined : true}
                onClick={(e) => isSingle ? openSource(product.sources[0].url) : openSourcesMenu(product, e)}
                data-test_id="Products__Name_Link"
            >
                {product.name}
                <i className={`${styles.linkedNameIcon} pi ${isSingle ? "pi-external-link" : "pi-angle-down"}`} />
            </button>
        );
    };

    const statusTemplate = (product: ProductOverview): React.ReactNode => {
        const presentation = getScanStatusPresentation(product.status);
        const elementId = `product-status-${product.id}`;
        // `statusText` lists one failing source per line, hence the pre-line tooltip.
        const tooltip = product.statusText != null
            ? `${presentation.label}\n${product.statusText}`
            : presentation.label;
        return (
            <>
                <Tooltip
                    target={`#${elementId}`}
                    content={tooltip}
                    className={styles.statusTooltip}
                    event="both"
                    position="left"
                />
                <Tag
                    id={elementId}
                    icon={presentation.icon}
                    severity={presentation.severity}
                    value={presentation.label}
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
                tooltipOptions={RowActionTooltipOptions}
                disabled={product.status === ProductScanStatus.Scanning}
                onClick={() => dispatch(store.Scans.Actions.scanProduct(product.id))}
                data-test_id="Products__Scan_Button"
            />
            <Button
                icon="pi pi-pencil"
                rounded text
                tooltip="Edit"
                tooltipOptions={RowActionTooltipOptions}
                onClick={() => openEditDialog(product)}
                data-test_id="Products__Edit_Button"
            />
            <Button
                icon="pi pi-trash"
                rounded text
                severity="danger"
                tooltip="Delete"
                tooltipOptions={RowActionTooltipOptions}
                onClick={() => deleteProduct(product)}
                data-test_id="Products__Delete_Button"
            />
        </div>
    );

    return (
        <LoadingSpinner target={LoadingTargets.Products}>
            {/* Shared by every row; `popup` keeps it closed until a name with several sources is
                clicked, and PrimeReact closes it again as soon as an item is chosen. */}
            <Menu
                model={sourcesMenuItems}
                popup
                ref={sourcesMenuRef}
                data-test_id="Products__Sources_Menu"
            />
            <DataTable
                value={overviews}
                dataKey="id"
                header={header}
                globalFilter={globalFilter}
                globalFilterFields={["name", "category", "description"]}
                sortMode="multiple"
                multiSortMeta={productsSortOrder}
                rowGroupMode="subheader"
                groupRowsBy="category"
                rowGroupHeaderTemplate={(product: ProductOverview) => (
                    <span className={styles.groupHeader} data-test_id="Products__Category_Group">{product.category ?? "Uncategorized"}</span>
                )}
                emptyMessage="No products yet. Add your first product to start tracking prices."
                className={`${styles.responsiveTable} ${styles.groupedTable}`}
                responsiveLayout="stack"
                breakpoint={MobileBreakpoint}
                data-test_id="Products__Table"
            >
                {/* The column widths are set on the header cells only: in the stacked layout the body
                    cells span the whole card and must not be constrained. */}
                <Column
                    body={statusTemplate}
                    header="Status"
                    headerStyle={{ width: "12rem" }}
                    bodyClassName={styles.cardStatus}
                />
                <Column
                    field="name"
                    header="Name"
                    sortable
                    body={nameTemplate}
                    bodyClassName={styles.cardTitle}
                    data-test_id="Products__Name"
                />
                <Column
                    field="lowestPrice"
                    header="Lowest Price"
                    sortable
                    align="right"
                    body={(p: ProductOverview) => formatPrice(p.lowestPrice)}
                    bodyClassName={(p: ProductOverview) => valueCellClass(formatPrice(p.lowestPrice))}
                    headerStyle={{ width: "10rem" }}
                />
                <Column
                    field="recentPrice"
                    header="Recent Price"
                    sortable
                    align="right"
                    body={(p: ProductOverview) => formatPrice(p.recentPrice)}
                    bodyClassName={(p: ProductOverview) => valueCellClass(formatPrice(p.recentPrice))}
                    headerStyle={{ width: "10rem" }}
                />
                <Column
                    field="targetPrice"
                    header="Target Price"
                    sortable
                    align="right"
                    body={(p: ProductOverview) => formatPrice(p.targetPrice)}
                    bodyClassName={(p: ProductOverview) => valueCellClass(formatPrice(p.targetPrice))}
                    headerStyle={{ width: "10rem" }}
                />
                <Column
                    field="lowestFoundDate"
                    header="Lowest Found On"
                    sortable
                    body={(p: ProductOverview) => formatDateFromTicks(p.lowestFoundDate)}
                    bodyClassName={(p: ProductOverview) => valueCellClass(formatDateFromTicks(p.lowestFoundDate))}
                    headerStyle={{ width: "11rem" }}
                />
                <Column
                    field="lastScannedDate"
                    header="Last Updated"
                    sortable
                    body={(p: ProductOverview) => formatDateFromTicks(p.lastScannedDate)}
                    bodyClassName={(p: ProductOverview) => valueCellClass(formatDateFromTicks(p.lastScannedDate))}
                    headerStyle={{ width: "11rem" }}
                />
                <Column body={actionsTemplate} headerStyle={{ width: "10rem" }} bodyClassName={styles.cardActions} />
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
