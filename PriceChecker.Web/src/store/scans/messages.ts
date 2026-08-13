import * as signalR from "@microsoft/signalr";
import { defaultLogger } from "@hwndmaster/atom-web-core";
import * as api from "@/api/api.generated";
import { convertProductOverviewApiToModel } from "@/api/converters/productsConverters";
import ScanProgress from "@/models/scanProgress";
import { ProductRef } from "@/models/types";
import { ApiUrl } from "@/shared/constants";
import * as productsActions from "@/store/products/actions";
import { getStore } from "@/store/setup";
import * as scansActions from "./actions";

const logger = defaultLogger;

let connection: signalR.HubConnection | null = null;

/**
 * Connects to the scan hub of the WebApi and translates the pushed messages
 * into store actions. Safe to call multiple times.
 */
export async function startScanHubConnection(): Promise<void> {
    if (connection != null) {
        return;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl(`${ApiUrl}/hubs/scan`)
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    connection.on("ProductScanStarted", (productId: ProductRef) => {
        getStore().dispatch(scansActions.productScanStarted(productId));
    });

    connection.on("ProductScanFailed", (productId: ProductRef, errorMessage: string) => {
        getStore().dispatch(scansActions.productScanFailed({ productId, errorMessage }));
    });

    connection.on("ProductScanFinished", (product: api.ProductOverviewDto) => {
        getStore().dispatch(scansActions.productScanFinished(convertProductOverviewApiToModel(product)));
    });

    connection.on("ScanProgress", (progress: ScanProgress) => {
        getStore().dispatch(scansActions.progressChanged(progress));
    });

    connection.onreconnected(() => {
        // Catch up with everything missed while being disconnected.
        getStore().dispatch(scansActions.fetchProgress());
        getStore().dispatch(productsActions.fetchProductOverviews());
    });

    try {
        await connection.start();
    } catch (error) {
        logger.error("Could not connect to the scan hub.", error);
        connection = null;
    }
}
