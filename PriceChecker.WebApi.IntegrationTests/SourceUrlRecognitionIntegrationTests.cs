using System.Net;
using Genius.PriceChecker.WebApi.IntegrationTests.Infrastructure;

namespace Genius.PriceChecker.WebApi.IntegrationTests;

public sealed class SourceUrlRecognitionIntegrationTests
{
    [Fact]
    public async Task SourceUrlRecognitionScenario()
    {
        /* Scenario Summary:
           1. Create an agent that states which URLs it accepts, and one that only has a URL template.
           2. Recognize a product URL in the shape a user copies out of the address bar.
           3. Recognize a URL that only the templated agent can serve.
           4. Recognize a URL of a site no agent knows.
           5. Recognize something that is not a web address.
           6. Create an agent whose URL pattern captures nothing and expect a validation problem. */

        // Arrange
        using var factory = new PriceCheckerWebApiFactory();
        using var httpClient = factory.CreateClient();

        // 1. Create the agents
        var amazonId = await httpClient.CreateAgentAsync("amazon.de", "https://www.amazon.de/gp/product/{0}?psc=1",
            urlPattern: @"^https?://(?:www\.)?amazon\.de/(?:[^/?#]+/)*(?:dp|gp/product)/(?<arg>[A-Z0-9]{10})");
        var tweakersId = await httpClient.CreateAgentAsync("tweakers.net", "https://tweakers.net/pricewatch/{0}");

        // 2. Recognize a URL as copied out of the address bar
        var (status, recognized) = await httpClient.PostJsonAsync("/api/v1/Agents/recognize", new
        {
            url = "https://www.amazon.de/Koch-Chemie-Wheel-Cleaner/dp/B0047AKFDG/ref=sr_1_3?keywords=koch",
        });
        Assert.Equal(HttpStatusCode.OK, status);
        var match = Assert.Single(recognized.EnumerateArray());
        Assert.Equal(amazonId, match.GetProperty("agentId").GetString());
        Assert.Equal("amazon.de", match.GetProperty("agentKey").GetString());
        Assert.Equal("B0047AKFDG", match.GetProperty("agentArgument").GetString());
        Assert.Equal(0, match.GetProperty("matchKind").GetInt32());  // UrlPattern

        // 3. Recognize a URL the templated agent serves
        (_, recognized) = await httpClient.PostJsonAsync("/api/v1/Agents/recognize", new
        {
            url = "https://tweakers.net/pricewatch/1432208/xbox-elite-series-2.html",
        });
        match = Assert.Single(recognized.EnumerateArray());
        Assert.Equal(tweakersId, match.GetProperty("agentId").GetString());
        Assert.Equal("1432208/xbox-elite-series-2.html", match.GetProperty("agentArgument").GetString());
        Assert.Equal(1, match.GetProperty("matchKind").GetInt32());  // UrlTemplate

        // 4. Recognize a URL of an unknown site
        (status, recognized) = await httpClient.PostJsonAsync("/api/v1/Agents/recognize", new
        {
            url = "https://www.mediamarkt.nl/nl/product/_xbox-elite-1234567.html",
        });
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Empty(recognized.EnumerateArray());

        // 5. Recognize something that is not a web address
        (status, recognized) = await httpClient.PostJsonAsync("/api/v1/Agents/recognize", new { url = "  " });
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Empty(recognized.EnumerateArray());

        // 6. Create an agent whose URL pattern captures no argument
        (status, _) = await httpClient.PostJsonAsync("/api/v1/Agents", new
        {
            key = "bol.com",
            url = "https://www.bol.com/nl/p/{0}",
            pricePattern = "some-pattern",
            handler = "SimpleRegex",
            decimalDelimiter = ".",
            urlPattern = @"^https?://(?:www\.)?bol\.com/nl/p/.+",
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }
}
