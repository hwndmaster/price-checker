using System.Text.Json;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Db;

/// <summary>
///   A one-time import of the data files created by the JSON-persistence-based (WPF) version
///   of the application. Runs only when the database contains no agents and no products.
/// </summary>
internal static class LegacyJsonDataImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task ImportAsync(PriceCheckerDbContext context, string legacyDataPath, ILogger logger)
    {
        Guard.NotNull(context);
        Guard.NotNullOrWhitespace(legacyDataPath);

        if (await context.Agents.AnyAsync().ConfigureAwait(false)
            || await context.Products.AnyAsync().ConfigureAwait(false))
        {
            return;
        }

        var agentsFile = Path.Combine(legacyDataPath, "Agent.json");
        var productsFile = Path.Combine(legacyDataPath, "Product.json");

        if (!File.Exists(agentsFile) && !File.Exists(productsFile))
        {
            logger.LogInformation("No legacy data files found at '{LegacyDataPath}', skipping the import.", legacyDataPath);
            return;
        }

        logger.LogInformation("Importing the legacy JSON data from '{LegacyDataPath}'.", legacyDataPath);

        var agentsByKey = await ImportAgentsAsync(context, agentsFile).ConfigureAwait(false);
        var (productCount, priceCount) = await ImportProductsAsync(context, productsFile, agentsByKey, logger).ConfigureAwait(false);

        await context.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("Legacy data import finished: {AgentCount} agents, {ProductCount} products, {PriceCount} price records.",
            agentsByKey.Count, productCount, priceCount);
    }

    private static async Task<Dictionary<string, Agent>> ImportAgentsAsync(PriceCheckerDbContext context, string agentsFile)
    {
        var agentsByKey = new Dictionary<string, Agent>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(agentsFile))
        {
            return agentsByKey;
        }

        var legacyAgents = await DeserializeFileAsync<LegacyAgent[]>(agentsFile).ConfigureAwait(false) ?? [];
        foreach (var legacyAgent in legacyAgents)
        {
            var agent = Agent.CreateWithNoLinking(
                legacyAgent.Key,
                legacyAgent.Url,
                legacyAgent.PricePattern,
                string.IsNullOrEmpty(legacyAgent.Handler) ? "SimpleRegex" : legacyAgent.Handler,
                string.IsNullOrEmpty(legacyAgent.DecimalDelimiter) ? '.' : legacyAgent.DecimalDelimiter[0],
                id: legacyAgent.Id);
            await context.Agents.AddAsync(agent).ConfigureAwait(false);
            agentsByKey[agent.Key] = agent;
        }

        return agentsByKey;
    }

    private static async Task<(int ProductCount, int PriceCount)> ImportProductsAsync(PriceCheckerDbContext context,
        string productsFile, Dictionary<string, Agent> agentsByKey, ILogger logger)
    {
        if (!File.Exists(productsFile))
        {
            return (0, 0);
        }

        var productCount = 0;
        var priceCount = 0;
        var legacyProducts = await DeserializeFileAsync<LegacyProduct[]>(productsFile).ConfigureAwait(false) ?? [];
        foreach (var legacyProduct in legacyProducts)
        {
            var product = Product.CreateWithNoLinking(legacyProduct.Name, legacyProduct.Category,
                legacyProduct.Description, id: legacyProduct.Id);
            await context.Products.AddAsync(product).ConfigureAwait(false);
            productCount++;

            var importedSourceIds = new HashSet<Guid>();
            foreach (var legacySource in legacyProduct.Sources ?? [])
            {
                if (!agentsByKey.TryGetValue(legacySource.AgentKey, out var agent))
                {
                    logger.LogWarning("Skipping the source '{SourceId}' of product '{ProductName}': the agent '{AgentKey}' is not known.",
                        legacySource.Id, legacyProduct.Name, legacySource.AgentKey);
                    continue;
                }

                await context.ProductSources.AddAsync(ProductSource.CreateWithNoLinking(
                    product.Id, agent.Id, legacySource.AgentArgument, id: legacySource.Id)).ConfigureAwait(false);
                importedSourceIds.Add(legacySource.Id);
            }

            // The legacy data kept the latest scan results per source plus a snapshot of the
            // lowest price ever found. Both become price history records.
            var legacyPrices = (legacyProduct.Recent ?? []).ToList();
            if (legacyProduct.Lowest is not null
                && !legacyPrices.Any(x => x.ProductSourceId == legacyProduct.Lowest.ProductSourceId
                    && x.FoundDate == legacyProduct.Lowest.FoundDate))
            {
                legacyPrices.Add(legacyProduct.Lowest);
            }

            foreach (var legacyPrice in legacyPrices)
            {
                if (!importedSourceIds.Contains(legacyPrice.ProductSourceId))
                {
                    continue;
                }

                await context.ProductPrices.AddAsync(ProductPrice.CreateWithNoLinking(
                    legacyPrice.ProductSourceId,
                    (AgentHandlingStatus)legacyPrice.Status,
                    legacyPrice.Price,
                    legacyPrice.FoundDate)).ConfigureAwait(false);
                priceCount++;
            }
        }

        return (productCount, priceCount);
    }

    private static async Task<T?> DeserializeFileAsync<T>(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions).ConfigureAwait(false);
    }

    private sealed record LegacyAgent(Guid Id, string Key, string Url, string PricePattern, string? Handler, string? DecimalDelimiter);
    private sealed record LegacyProductSource(Guid Id, string AgentKey, string AgentArgument);
    private sealed record LegacyProductPrice(Guid ProductSourceId, int Status, decimal? Price, DateTimeOffset FoundDate);
    private sealed record LegacyProduct(Guid Id, string Name, string? Category, string? Description,
        LegacyProductSource[]? Sources, LegacyProductPrice? Lowest, LegacyProductPrice[]? Recent);
}
