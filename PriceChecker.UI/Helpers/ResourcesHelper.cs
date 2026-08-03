using System.Windows.Media.Imaging;
using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.UI.Helpers;

public static class ResourcesHelper
{
    public static string? GetStatusIconUri(ProductScanStatus status)
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
        return GetIconUri(icon);
    }

    public static string? GetIconUri(string? resourceName)
    {
        if (resourceName is null)
            return null;

        // The icon properties of the view models are string-typed (a requirement of the AutoGrid
        // builder API), so the resource is translated to its pack URI, which WPF converts back
        // to an image when binding.
        return ((BitmapImage)App.Current.FindResource(resourceName)).UriSource?.ToString();
    }
}
