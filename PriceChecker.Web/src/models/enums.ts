/**
 * The outcome of handling a single product source by a scanning agent.
 * Must be kept in sync with `Genius.PriceChecker.Dto.AgentHandlingStatus`.
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
 * Must be kept in sync with `Genius.PriceChecker.Dto.ProductScanStatus`.
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

/**
 * How an agent was matched against a pasted product URL.
 * Must be kept in sync with `Genius.PriceChecker.Dto.SourceUrlMatchKind`.
 */
export enum SourceUrlMatchKind {
    /** The agent's own URL pattern matched. */
    UrlPattern = 0,
    /** The agent has no URL pattern and its URL template was matched in reverse. */
    UrlTemplate = 1,
}
