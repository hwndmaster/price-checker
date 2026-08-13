import React, { useEffect, useRef } from "react";
import { toastService } from "@hwndmaster/atom-react-prime";
import { ProgressBar, Tooltip } from "@/primereact";
import * as store from "@/store";
import styles from "./scanProgressBar.module.scss";

/**
 * Shows the progress of the currently running price scan, fed by the live scan hub updates.
 * Notifies with a toast when the scan has finished.
 */
const ScanProgressBar: React.FC = () => {
    const progress = store.useAppSelector((state) => state.scans.progress);
    const previousIsFinished = useRef(true);

    useEffect(() => {
        if (progress == null) {
            return;
        }

        if (!previousIsFinished.current && progress.isFinished) {
            const message = progress.hasNewLowestPrice
                ? "Prices for some products have become even lower! Check it out."
                : "Nothing interesting has been caught.";
            if (progress.hasErrors) {
                toastService.showWarn("Scan finished",
                    `${message} NOTE: Some products could not finish scanning properly. Check the logs for details.`);
            } else {
                toastService.showInfo("Scan finished", message);
            }
        }

        previousIsFinished.current = progress.isFinished;
    }, [progress]);

    if (progress == null || progress.isFinished || progress.total === 0) {
        return null;
    }

    const percentage = Math.round((progress.finished / progress.total) * 100);

    return (
        <>
            <Tooltip target={`.${styles.scanProgress}`} content={`Scanning: ${progress.finished} of ${progress.total} products`} />
            <ProgressBar
                className={styles.scanProgress}
                value={percentage}
                showValue={false}
                color={progress.hasErrors ? "var(--orange-500)" : undefined}
                data-test_id="Layout__Scan_Progress"
            />
        </>
    );
};

export default ScanProgressBar;
