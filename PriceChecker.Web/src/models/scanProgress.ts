/**
 * The overall progress of the currently running price scan.
 */
interface ScanProgress {
    finished: number;
    total: number;
    hasErrors: boolean;
    hasNewLowestPrice: boolean;
    isFinished: boolean;
}

export default ScanProgress;
