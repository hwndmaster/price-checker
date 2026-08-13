import { createAction } from "@reduxjs/toolkit";
import ScanProgress from "@/models/scanProgress";

export const setProgress = createAction<ScanProgress>("scans/setProgress");
