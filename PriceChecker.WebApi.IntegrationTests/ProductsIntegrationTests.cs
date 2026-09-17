using System.Net;
using Genius.PriceChecker.WebApi.IntegrationTests.Infrastructure;

namespace Genius.PriceChecker.WebApi.IntegrationTests;

public sealed class ProductsIntegrationTests
{
    [Fact]
    public async Task ProductsWorkflowScenario()
    {
        /* Scenario Summary:
           1. Create an agent to be used by the product sources.
           2. Create a product with a source over the API.
           3. Fetch the product overview and find the created product with the NotScanned status.
           4. Fetch the single product and verify its source.
           5. Create a product referencing an unknown agent and expect a validation problem.
           6. Verify the scan progress endpoint responds with the initial (finished) state. */

        // Arrange
        using var factory = new PriceCheckerWebApiFactory();
        using var httpClient = factory.CreateClient();

        // 1. Create an agent
        var agentId = await httpClient.CreateAgentAsync("amazon.de", "https://www.amazon.de/gp/product/{0}");

        // 2. Create a product with a source
        var (productStatus, createdProduct) = await httpClient.PostJsonAsync("/api/v1/Products", new
        {
            name = "Roomba",
            category = "Household",
            description = (string?)null,
            sources = new[] { new { agentId, agentArgument = "B000123" } },
        });
        Assert.Equal(HttpStatusCode.OK, productStatus);
        var productId = createdProduct.GetProperty("entityId").GetString();

        // 3. Fetch the overview
        var overviews = await httpClient.GetJsonAsync("/api/v1/Products/overview");
        var overview = Assert.Single(overviews.EnumerateArray());
        Assert.Equal("Roomba", overview.GetProperty("name").GetString());
        Assert.Equal(0, overview.GetProperty("status").GetInt32());  // NotScanned

        // 4. Fetch the single product
        var product = await httpClient.GetJsonAsync($"/api/v1/Products/{productId}");
        var source = Assert.Single(product.GetProperty("sources").EnumerateArray());
        Assert.Equal(agentId, source.GetProperty("agentId").GetString());
        Assert.Equal("B000123", source.GetProperty("agentArgument").GetString());

        // 5. Create a product with an unknown agent
        var (invalidStatus, _) = await httpClient.PostJsonAsync("/api/v1/Products", new
        {
            name = "Invalid",
            category = (string?)null,
            description = (string?)null,
            sources = new[] { new { agentId = Guid.NewGuid().ToString(), agentArgument = "X" } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus);

        // 6. Verify the scan progress endpoint
        var progress = await httpClient.GetJsonAsync("/api/v1/Scans/progress");
        Assert.True(progress.GetProperty("isFinished").GetBoolean());
    }
}
