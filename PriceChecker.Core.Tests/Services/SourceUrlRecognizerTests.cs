using Genius.Atom.Infrastructure.TestingUtil;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;
using Genius.PriceChecker.Dto;
using Genius.PriceChecker.Dto.References;

namespace Genius.PriceChecker.Core.Tests.Services;

public sealed class SourceUrlRecognizerTests
{
    private const string AmazonDeUrlPattern =
        @"^https?://(?:www\.)?amazon\.de/(?:[^/?#]+/)*(?:dp|gp/product)/(?<arg>[A-Z0-9]{10})";

    private readonly SourceUrlRecognizer _sut = new(new FakeLogger<SourceUrlRecognizer>());

    [Theory]
    // The canonical URL the agent's own template builds.
    [InlineData("https://www.amazon.de/gp/product/B0047AKFDG?psc=1", "B0047AKFDG")]
    // The URL a user actually copies out of the address bar.
    [InlineData("https://www.amazon.de/Koch-Chemie-Wheel-Cleaner/dp/B0047AKFDG/ref=sr_1_3?keywords=koch", "B0047AKFDG")]
    [InlineData("https://www.amazon.de/dp/B082WD5YV9", "B082WD5YV9")]
    [InlineData("http://amazon.de/dp/B082WD5YV9", "B082WD5YV9")]
    // Pasted without a scheme.
    [InlineData("www.amazon.de/dp/B082WD5YV9", "B082WD5YV9")]
    public void Recognize__Url_pattern_matches__Reads_the_argument_off_the_url(string url, string expectedArgument)
    {
        // Arrange
        var agent = CreateAgent("amazon.de", "https://www.amazon.de/gp/product/{0}?psc=1", AmazonDeUrlPattern);

        // Act
        var result = _sut.Recognize(url, [agent]);

        // Assert
        var recognized = Assert.Single(result);
        Assert.Equal(agent.AgentId, recognized.AgentId);
        Assert.Equal("amazon.de", recognized.AgentKey);
        Assert.Equal(expectedArgument, recognized.AgentArgument);
        Assert.Equal(SourceUrlMatchKind.UrlPattern, recognized.MatchKind);
    }

    [Theory]
    [InlineData("https://www.amazon.nl/dp/B0047AKFDG")]
    [InlineData("https://www.amazon.de/gp/help/customer/display.html")]
    public void Recognize__Url_belongs_to_another_site_or_page__Returns_nothing(string url)
    {
        // Arrange
        var agent = CreateAgent("amazon.de", "https://www.amazon.de/gp/product/{0}?psc=1", AmazonDeUrlPattern);

        // Act
        var result = _sut.Recognize(url, [agent]);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    // The template's own trailing query is not expected on a pasted URL, ...
    [InlineData("https://tweakers.net/pricewatch/1432208/xbox-elite-series-2.html", "1432208/xbox-elite-series-2.html")]
    // ... nor is a query of its own, a fragment or a trailing slash a part of the argument.
    [InlineData("https://tweakers.net/pricewatch/1432208/xbox-elite-series-2.html?nb=1", "1432208/xbox-elite-series-2.html")]
    [InlineData("https://tweakers.net/pricewatch/1432208/xbox#reviews", "1432208/xbox")]
    [InlineData("https://www.tweakers.net/pricewatch/1432208/xbox/", "1432208/xbox")]
    public void Recognize__Agent_has_no_url_pattern__Falls_back_to_the_url_template(string url, string expectedArgument)
    {
        // Arrange
        var agent = CreateAgent("tweakers.net", "https://tweakers.net/pricewatch/{0}", urlPattern: null);

        // Act
        var result = _sut.Recognize(url, [agent]);

        // Assert
        var recognized = Assert.Single(result);
        Assert.Equal(expectedArgument, recognized.AgentArgument);
        Assert.Equal(SourceUrlMatchKind.UrlTemplate, recognized.MatchKind);
    }

    [Fact]
    public void Recognize__Several_agents_match__Orders_the_explicit_pattern_first()
    {
        // Arrange
        var withTemplate = CreateAgent("amazon.de_legacy", "https://www.amazon.de/gp/product/{0}", urlPattern: null);
        var withPattern = CreateAgent("amazon.de", "https://www.amazon.de/gp/product/{0}?psc=1", AmazonDeUrlPattern);

        // Act
        var result = _sut.Recognize("https://www.amazon.de/gp/product/B0047AKFDG", [withTemplate, withPattern]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("amazon.de", result[0].AgentKey);
        Assert.Equal(SourceUrlMatchKind.UrlPattern, result[0].MatchKind);
        Assert.Equal("amazon.de_legacy", result[1].AgentKey);
        Assert.Equal(SourceUrlMatchKind.UrlTemplate, result[1].MatchKind);
    }

    [Fact]
    public void Recognize__Two_templates_of_the_same_site_match__Orders_the_more_specific_one_first()
    {
        // Arrange
        var general = CreateAgent("viofo.nl_all", "https://www.viofo.nl/en/{0}", urlPattern: null);
        var specific = CreateAgent("viofo.nl", "https://www.viofo.nl/en/collections/viofo-dashcams/products/{0}", urlPattern: null);

        // Act
        var result = _sut.Recognize("https://www.viofo.nl/en/collections/viofo-dashcams/products/a229-pro", [general, specific]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("viofo.nl", result[0].AgentKey);
        Assert.Equal("a229-pro", result[0].AgentArgument);
        Assert.Equal("viofo.nl_all", result[1].AgentKey);
        Assert.Equal("collections/viofo-dashcams/products/a229-pro", result[1].AgentArgument);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("B0047AKFDG")]
    [InlineData("ftp://www.amazon.de/dp/B0047AKFDG")]
    [InlineData("javascript:alert(1)")]
    public void Recognize__Input_is_not_a_web_address__Returns_nothing(string? url)
    {
        // Arrange
        var agent = CreateAgent("amazon.de", "https://www.amazon.de/gp/product/{0}?psc=1", AmazonDeUrlPattern);

        // Act
        var result = _sut.Recognize(url, [agent]);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    // Not a valid regular expression.
    [InlineData(@"^https?://(?:www\.)?amazon\.de/(?<arg>[A-Z0-9]{10}")]
    // Valid, but captures nothing to use as the agent argument.
    [InlineData(@"^https?://(?:www\.)?amazon\.de/dp/[A-Z0-9]{10}")]
    public void Recognize__Url_pattern_is_unusable__Leaves_out_only_that_agent(string urlPattern)
    {
        // Arrange
        var broken = CreateAgent("amazon.de_broken", "https://www.amazon.de/gp/product/{0}", urlPattern);
        var sound = CreateAgent("amazon.de", "https://www.amazon.de/gp/product/{0}?psc=1", AmazonDeUrlPattern);

        // Act
        var result = _sut.Recognize("https://www.amazon.de/dp/B0047AKFDG", [broken, sound]);

        // Assert
        var recognized = Assert.Single(result);
        Assert.Equal("amazon.de", recognized.AgentKey);
    }

    [Theory]
    // Without a placeholder the template resolves to the same URL for every argument.
    [InlineData("https://www.example.com/products")]
    // A placeholder inside the host leaves no literal site to match against.
    [InlineData("https://{0}.example.com/products")]
    [InlineData("not-a-url/{0}")]
    public void Recognize__Url_template_cannot_be_reversed__Returns_nothing(string urlTemplate)
    {
        // Arrange
        var agent = CreateAgent("odd", urlTemplate, urlPattern: null);

        // Act
        var result = _sut.Recognize("https://www.example.com/products/42", [agent]);

        // Assert
        Assert.Empty(result);
    }

    private static RecognizableAgent CreateAgent(string key, string urlTemplate, string? urlPattern)
        => new(new AgentRef(Guid.NewGuid()), key, urlTemplate, urlPattern);
}
