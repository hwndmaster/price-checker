/**
 * The outcome of handling a single product source by a scanning agent.
 * Must be kept in sync with `Genius.PriceChecker.Core.Models.AgentHandlingStatus`.
 */
export enum AgentHandlingStatus {
    Unknown = 0,
    Success = 1,
    CouldNotFetch = 2,
    CouldNotMatch = 3,
    CouldNotParse = 4,
    InvalidPrice = 5,
}

/**
 * The scan status of a product.
 * Must be kept in sync with `Genius.PriceChecker.Core.Models.ProductScanStatus`.
 */
export enum ProductScanStatus {
    NotScanned = 0,
    Scanning = 1,
    ScannedOk = 2,
    ScannedWithErrors = 3,
    ScannedNewLowest = 4,
    Outdated = 5,
    Failed = 6,
}
