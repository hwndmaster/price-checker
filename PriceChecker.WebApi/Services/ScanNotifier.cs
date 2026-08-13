using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;
using Genius.PriceChecker.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Genius.PriceChecker.WebApi.Services;

public interface IScanNotifier
{
    Task ProductScanStartedAsync(ProductRef productId);
    Task ProductScanFailedAsync(ProductRef productId, string errorMessage);
    Task ProductScanFinishedAsync(ProductOverviewDto product);
    Task ScanProgressAsync(ScanProgressDto progress);
}

internal sealed class ScanHubNotifier : IScanNotifier
{
    internal const string ProductScanStartedMessage = "ProductScanStarted";
    internal const string ProductScanFailedMessage = "ProductScanFailed";
    internal const string ProductScanFinishedMessage = "ProductScanFinished";
    internal const string ScanProgressMessage = "ScanProgress";

    private readonly IHubContext<ScanHub> _hubContext;

    public ScanHubNotifier(IHubContext<ScanHub> hubContext)
    {
        _hubContext = hubContext.NotNull();
    }

    public Task ProductScanStartedAsync(ProductRef productId)
        => _hubContext.Clients.All.SendAsync(ProductScanStartedMessage, productId);

    public Task ProductScanFailedAsync(ProductRef productId, string errorMessage)
        => _hubContext.Clients.All.SendAsync(ProductScanFailedMessage, productId, errorMessage);

    public Task ProductScanFinishedAsync(ProductOverviewDto product)
        => _hubContext.Clients.All.SendAsync(ProductScanFinishedMessage, product);

    public Task ScanProgressAsync(ScanProgressDto progress)
        => _hubContext.Clients.All.SendAsync(ScanProgressMessage, progress);
}
