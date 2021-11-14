using System.Windows.Media.Imaging;
using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.UI.Helpers;

public static class ResourcesHelper
{
    public static BitmapImage? GetStatusIcon(ProductScanStatus status)
    {
        var icon = status switch
        {
            ProductScanStatus.NotScanned => "Unknown16",
            ProductScanStatus.Scanning => "Loading32",
            ProductScanStatus.ScannedOk => "DonePink16",
            ProductScanStatus.ScannedWithErrors => "Warning16",
            ProductScanStatus.ScannedNewLowest => "Dance32",
            ProductScanStatus.Outdated => "Outdated16",
            ProductScanStatus.Failed => "Error16",
            {} => null
        };
        if (icon == null)
            return null;
        return (BitmapImage)App.Current.FindResource(icon);
    }
}
