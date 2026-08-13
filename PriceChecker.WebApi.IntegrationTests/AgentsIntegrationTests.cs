using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Genius.PriceChecker.WebApi.IntegrationTests.Infrastructure;

namespace Genius.PriceChecker.WebApi.IntegrationTests;

public sealed class AgentsIntegrationTests
{
    [Fact]
    public async Task AgentsWorkflowScenario()
    {
        /* Scenario Summary:
           1. Create an agent over the API.
           2. Fetch all agents and find the created one.
           3. Update the agent with the correct version token.
           4. Update the agent again with a stale version token and expect a version conflict.
           5. Create another agent with a duplicate key and expect a validation problem.
           6. Delete the agent. */

        // Arrange
        using var factory = new PriceCheckerWebApiFactory();
        using var httpClient = factory.CreateClient();

        // 1. Create an agent
        var createResponse = await httpClient.PostAsJsonAsync("/api/v1/Agents", new
        {
            key = "amazon.de",
            url = "https://www.amazon.de/gp/product/{0}",
            pricePattern = "some-pattern",
            handler = "SimpleRegex",
            decimalDelimiter = ".",
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        var agentId = created.GetProperty("entityId").GetString();
        var lastModified = created.GetProperty("lastModified").GetInt64();
        Assert.NotNull(agentId);

        // 2. Fetch all agents
        var agents = JsonDocument.Parse(await httpClient.GetStringAsync("/api/v1/Agents", TestContext.Current.CancellationToken)).RootElement;
        var agent = Assert.Single(agents.EnumerateArray());
        Assert.Equal("amazon.de", agent.GetProperty("key").GetString());

        // 3. Update the agent
        var updateResponse = await httpClient.PutAsJsonAsync("/api/v1/Agents", new
        {
            id = agentId,
            lastModified,
            key = "amazon.nl",
            url = "https://www.amazon.nl/gp/product/{0}",
            pricePattern = "other-pattern",
            handler = "SimpleRegexDivideBy100",
            decimalDelimiter = ",",
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // 4. Update with a stale version token
        var conflictResponse = await httpClient.PutAsJsonAsync("/api/v1/Agents", new
        {
            id = agentId,
            lastModified = lastModified - 1,
            key = "amazon.nl",
            url = "https://www.amazon.nl/gp/product/{0}",
            pricePattern = "other-pattern",
            handler = "SimpleRegex",
            decimalDelimiter = ",",
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        var conflict = JsonDocument.Parse(await conflictResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        Assert.Equal("Version conflict", conflict.GetProperty("title").GetString());

        // 5. Create an agent with a duplicate key
        var duplicateResponse = await httpClient.PostAsJsonAsync("/api/v1/Agents", new
        {
            key = "amazon.nl",
            url = "https://www.amazon.nl/gp/product/{0}",
            pricePattern = "some-pattern",
            handler = "SimpleRegex",
            decimalDelimiter = ".",
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);

        // 6. Delete the agent
        var deleteResponse = await httpClient.DeleteAsync($"/api/v1/Agents/{agentId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        agents = JsonDocument.Parse(await httpClient.GetStringAsync("/api/v1/Agents", TestContext.Current.CancellationToken)).RootElement;
        Assert.Empty(agents.EnumerateArray());
    }
}
