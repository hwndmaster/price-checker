namespace Genius.PriceChecker.Core.Models;

public static class ModelExtensions
{
    public static ProductPrice? MinPrice(this Product product)
    {
        var successfulResults = product.Recent
            .Where(x => x.Status == AgentHandlingStatus.Success)
            .ToList();
        if (!successfulResults.Any())
        {
            return null;
        }

        return successfulResults.MinBy(x => x.Price);
    }
}
