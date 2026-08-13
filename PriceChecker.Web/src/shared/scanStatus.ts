import { ProductScanStatus } from "@/models/enums";

interface ScanStatusPresentation {
    label: string;
    severity: "success" | "info" | "warning" | "danger" | "secondary";
    icon: string;
}

const presentations: Record<ProductScanStatus, ScanStatusPresentation> = {
    [ProductScanStatus.NotScanned]: { label: "Not scanned", severity: "secondary", icon: "pi pi-question" },
    [ProductScanStatus.Scanning]: { label: "Scanning", severity: "info", icon: "pi pi-spin pi-spinner" },
    [ProductScanStatus.ScannedOk]: { label: "Scanned", severity: "success", icon: "pi pi-check" },
    [ProductScanStatus.ScannedWithErrors]: { label: "Scanned with errors", severity: "warning", icon: "pi pi-exclamation-triangle" },
    [ProductScanStatus.ScannedNewLowest]: { label: "New lowest price!", severity: "success", icon: "pi pi-arrow-down" },
    [ProductScanStatus.Outdated]: { label: "Outdated", severity: "secondary", icon: "pi pi-clock" },
    [ProductScanStatus.Failed]: { label: "Failed", severity: "danger", icon: "pi pi-times" },
};

/**
 * Returns the visual presentation (label, severity, icon) of a product scan status.
 * @param status The product scan status.
 * @returns The presentation of the status.
 */
export function getScanStatusPresentation(status: ProductScanStatus): ScanStatusPresentation {
    return presentations[status] ?? presentations[ProductScanStatus.NotScanned];
}
