import { createSelector } from "@reduxjs/toolkit";
import ScanProgress from "@/models/scanProgress";
import AppState from "@/store/appState";

export const selectIsScanRunning: (state: AppState) => boolean = createSelector(
    [(state: AppState): ScanProgress | null => state.scans.progress],
    (progress) => progress != null && !progress.isFinished
);
