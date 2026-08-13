using Microsoft.AspNetCore.SignalR;

namespace Genius.PriceChecker.WebApi.Hubs;

/// <summary>
///   Pushes the price scanning updates to the connected clients. The clients do not invoke
///   anything on the hub; scans are triggered over the REST endpoints.
/// </summary>
public sealed class ScanHub : Hub
{
}
