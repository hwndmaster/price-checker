namespace Genius.PriceChecker.Core.Models
{
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
}
