using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
        var agentResponse = await httpClient.PostAsJsonAsync("/api/v1/Agents", new
        {
            key = "amazon.de",
            url = "https://www.amazon.de/gp/product/{0}",
            pricePattern = "some-pattern",
            handler = "SimpleRegex",
            decimalDelimiter = ".",
        }, TestContext.Current.CancellationToken);
        var agentId = JsonDocument.Parse(await agentResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("entityId").GetString();

        // 2. Create a product with a source
        var productResponse = await httpClient.PostAsJsonAsync("/api/v1/Products", new
        {
            name = "Roomba",
            category = "Household",
            description = (string?)null,
            sources = new[] { new { agentId, agentArgument = "B000123" } },
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, productResponse.StatusCode);
        var productId = JsonDocument.Parse(await productResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.GetProperty("entityId").GetString();

        // 3. Fetch the overview
        var overviews = JsonDocument.Parse(await httpClient.GetStringAsync("/api/v1/Products/overview", TestContext.Current.CancellationToken)).RootElement;
        var overview = Assert.Single(overviews.EnumerateArray());
        Assert.Equal("Roomba", overview.GetProperty("name").GetString());
        Assert.Equal(0, overview.GetProperty("status").GetInt32());  // NotScanned

        // 4. Fetch the single product
        var product = JsonDocument.Parse(await httpClient.GetStringAsync($"/api/v1/Products/{productId}", TestContext.Current.CancellationToken)).RootElement;
        var source = Assert.Single(product.GetProperty("sources").EnumerateArray());
        Assert.Equal(agentId, source.GetProperty("agentId").GetString());
        Assert.Equal("B000123", source.GetProperty("agentArgument").GetString());

        // 5. Create a product with an unknown agent
        var invalidResponse = await httpClient.PostAsJsonAsync("/api/v1/Products", new
        {
            name = "Invalid",
            category = (string?)null,
            description = (string?)null,
            sources = new[] { new { agentId = Guid.NewGuid().ToString(), agentArgument = "X" } },
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        // 6. Verify the scan progress endpoint
        var progress = JsonDocument.Parse(await httpClient.GetStringAsync("/api/v1/Scans/progress", TestContext.Current.CancellationToken)).RootElement;
        Assert.True(progress.GetProperty("isFinished").GetBoolean());
    }
}
