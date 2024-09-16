using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.UI.Helpers;

public interface IProductInteraction
{
    Task ShowProductInBrowserAsync(ProductSource? productSource);
}

[ExcludeFromCodeCoverage]
public class ProductInteraction : IProductInteraction
{
    private readonly IAgentQueryService _agentQuery;

    public ProductInteraction(IAgentQueryService agentQuery)
    {
        _agentQuery = agentQuery;
    }

    public async Task ShowProductInBrowserAsync(ProductSource? productSource)
    {
        if (productSource == null)
            return;

        var agent = await _agentQuery.FindByKeyAsync(productSource.AgentKey);
        if (agent?.Url is null)
        {
            return;
        }
        var url = string.Format(CultureInfo.CurrentCulture, agent.Url, productSource.AgentArgument);

        url = url.Replace("&", "^&");
        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
    }
}
