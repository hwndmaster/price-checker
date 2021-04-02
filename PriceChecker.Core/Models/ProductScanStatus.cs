namespace Genius.PriceChecker.Core.Models
{
    public enum ProductScanStatus
    {
        NotScanned,
        Scanning,
        ScannedOk,
        ScannedNewLowest,
        Outdated,
        Failed
    }
}
