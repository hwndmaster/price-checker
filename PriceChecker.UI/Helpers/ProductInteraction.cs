using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;

namespace Genius.PriceChecker.UI.Helpers;

public interface IProductInteraction
{
    void ShowProductInBrowser(ProductSource? productSource);
}

[ExcludeFromCodeCoverage]
public class ProductInteraction : IProductInteraction
{
    private readonly IAgentQueryService _agentQuery;

    public ProductInteraction(IAgentQueryService agentQuery)
    {
        _agentQuery = agentQuery;
    }

    public void ShowProductInBrowser(ProductSource? productSource)
    {
        if (productSource == null)
            return;

        var agentUrl = _agentQuery.FindByKey(productSource.AgentKey)?.Url;
        if (agentUrl is null)
        {
            return;
        }
        var url = string.Format(agentUrl, productSource.AgentArgument);

        url = url.Replace("&", "^&");
        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
    }
}
