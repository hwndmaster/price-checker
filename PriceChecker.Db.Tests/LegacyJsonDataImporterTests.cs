using Genius.Atom.Infrastructure.TestingUtil;
using Microsoft.EntityFrameworkCore;

namespace Genius.PriceChecker.Db.Tests;

public sealed class LegacyJsonDataImporterTests : IDisposable
{
    private readonly string _legacyDataPath = Path.Combine(Path.GetTempPath(), $"pricechecker-import-{Guid.NewGuid()}");

    public LegacyJsonDataImporterTests()
    {
        Directory.CreateDirectory(_legacyDataPath);
    }

    public void Dispose()
    {
        Directory.Delete(_legacyDataPath, recursive: true);
    }

    [Fact]
    public async Task ImportAsync_GivenLegacyJsonFiles_WhenImported_ThenEntitiesAndPricesAreStored()
    {
        // Arrange
        /* 1. A legacy agent, a product with one source referencing the agent by key,
              a recent price and a distinct lowest price snapshot. */
        const string AgentId = "6f35fbc4-4e3b-4734-a32e-66bdcc904d15";
        const string ProductId = "6a52bd42-d19e-4572-9295-28e01e7e7417";
        const string SourceId = "60b9ceed-b34c-45ad-a142-af0ec5491d91";
        await File.WriteAllTextAsync(Path.Combine(_legacyDataPath, "Agent.json"), $$"""
            [
              {
                "Key": "amazon.de",
                "Url": "https://www.amazon.de/gp/product/{0}?psc=1",
                "PricePattern": "some-pattern",
                "Handler": "SimpleRegex",
                "DecimalDelimiter": ".",
                "Id": "{{AgentId}}"
              }
            ]
            """, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_legacyDataPath, "Product.json"), $$"""
            [
              {
                "Category": "Household",
                "Name": "Roomba",
                "Description": null,
                "Sources": [
                  { "Id": "{{SourceId}}", "AgentKey": "amazon.de", "AgentArgument": "B000123" }
                ],
                "Lowest": { "ProductSourceId": "{{SourceId}}", "Status": 1, "Price": 699, "FoundDate": "2022-01-17T22:11:30.3167162+01:00" },
                "Recent": [
                  { "ProductSourceId": "{{SourceId}}", "Status": 1, "Price": 749, "FoundDate": "2022-05-17T22:11:30.3167121+01:00" }
                ],
                "Id": "{{ProductId}}",
                "DateCreated": "0001-01-01T00:00:00+00:00",
                "LastModified": "0001-01-01T00:00:00+00:00"
              }
            ]
            """, TestContext.Current.CancellationToken);

        await using var context = new RepositoryTestContext();

        // Act
        /* 2. Run the import over an empty database. */
        await LegacyJsonDataImporter.ImportAsync(context.DbContext, _legacyDataPath, new FakeLogger());

        // Assert
        /* 3. The agent, the product with its source and both price records (recent + lowest snapshot)
              ended up in the database, keeping the original identifiers. */
        var agent = Assert.Single(await context.DbContext.Agents.ToArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal(new Guid(AgentId), agent.Id.Id);
        Assert.Equal("amazon.de", agent.Key);

        var product = Assert.Single(await context.DbContext.Products.Include(p => p.Sources).ToArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal(new Guid(ProductId), product.Id.Id);
        var source = Assert.Single(product.Sources);
        Assert.Equal(new Guid(SourceId), source.Id.Id);
        Assert.Equal(agent.Id, source.AgentId);

        var prices = await context.DbContext.ProductPrices.ToArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, prices.Length);
        Assert.Contains(prices, p => p.Price == 749);
        Assert.Contains(prices, p => p.Price == 699);
    }

    [Fact]
    public async Task ImportAsync_GivenNonEmptyDatabase_WhenImported_ThenNothingHappens()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_legacyDataPath, "Agent.json"),
            """[ { "Key": "a", "Url": "u", "PricePattern": "p", "Handler": "SimpleRegex", "DecimalDelimiter": ".", "Id": "6f35fbc4-4e3b-4734-a32e-66bdcc904d15" } ]""",
            TestContext.Current.CancellationToken);
        await using var context = new RepositoryTestContext();
        context.DbContext.Agents.Add(Models.Agent.CreateWithNoLinking("existing", "url", "pattern", "SimpleRegex", '.'));
        await context.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await LegacyJsonDataImporter.ImportAsync(context.DbContext, _legacyDataPath, new FakeLogger());

        // Assert
        var agent = Assert.Single(await context.DbContext.Agents.ToArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal("existing", agent.Key);
    }
}
