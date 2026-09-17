using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Genius.PriceChecker.WebApi.IntegrationTests.Infrastructure;

/// <summary>
///   The request and response plumbing every scenario repeats. Anything a single scenario needs
///   stays in that scenario.
/// </summary>
internal static class ApiTestExtensions
{
    /// <summary>
    ///   Reads the response body as JSON. The cancellation token of the running test is implied.
    /// </summary>
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        var body = await response.NotNull().Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(body).RootElement;
    }

    /// <summary>
    ///   Fetches the given endpoint and reads its body as JSON.
    /// </summary>
    public static async Task<JsonElement> GetJsonAsync(this HttpClient httpClient, string requestUri)
    {
        var body = await httpClient.NotNull().GetStringAsync(requestUri, TestContext.Current.CancellationToken);
        return JsonDocument.Parse(body).RootElement;
    }

    /// <summary>
    ///   Posts the given payload and reads the response body as JSON.
    /// </summary>
    public static async Task<(HttpStatusCode Status, JsonElement Body)> PostJsonAsync<T>(
        this HttpClient httpClient, string requestUri, T payload)
    {
        var response = await httpClient.NotNull().PostAsJsonAsync(requestUri, payload, TestContext.Current.CancellationToken);
        return (response.StatusCode, await response.ReadJsonAsync());
    }

    /// <summary>
    ///   Creates an agent the scenario only needs in order to hang something else off it, and
    ///   returns its id.
    /// </summary>
    public static async Task<string> CreateAgentAsync(this HttpClient httpClient, string key, string url,
        string? urlPattern = null, string pricePattern = "some-pattern", string decimalDelimiter = ".")
    {
        var (status, body) = await httpClient.PostJsonAsync("/api/v1/Agents", new
        {
            key,
            url,
            pricePattern,
            handler = "SimpleRegex",
            decimalDelimiter,
            urlPattern,
        });

        Assert.Equal(HttpStatusCode.OK, status);
        var agentId = body.GetProperty("entityId").GetString();
        Assert.NotNull(agentId);
        return agentId;
    }
}
