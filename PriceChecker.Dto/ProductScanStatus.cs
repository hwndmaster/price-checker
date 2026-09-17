namespace Genius.PriceChecker.Dto;

public enum ProductScanStatus
{
    NotScanned,
    Scanning,
    ScannedOk,
    ScannedWithErrors,
    ScannedNewLowest,
    Outdated,
    Failed
}
