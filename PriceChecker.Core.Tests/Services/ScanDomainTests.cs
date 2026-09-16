using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Services;

namespace Genius.PriceChecker.Core.Tests.Services;

public sealed class ScanDomainTests
{
    [Fact]
    public void Resolve_GivenAnAgentUrl_WhenResolved_ThenReturnsTheHost()
    {
        // Act
        var domain = ScanDomain.Resolve("https://tweakers.net/pricewatch/{0}", "1617418/some-product.html");

        // Assert
        Assert.Equal("tweakers.net", domain);
    }

    [Fact]
    public void Resolve_GivenSeveralAgentsOfTheSameSite_WhenResolved_ThenTheyShareTheDomain()
    {
        // Arrange
        // Three agents differing only in the handler and the pattern, all fetching the same site.
        var urls = new[]
        {
            "https://www.amazon.de/gp/product/{0}?psc=1",
            "https://www.amazon.de/dp/{0}",
            "https://amazon.de/gp/product/{0}",
        };

        // Act
        var domains = urls.Select(url => ScanDomain.Resolve(url, "B0047AKFDG")).Distinct(StringComparer.Ordinal);

        // Assert
        Assert.Equal(["amazon.de"], domains);
    }

    [Fact]
    public void Resolve_GivenTheSameSiteInAnotherCountry_WhenResolved_ThenTheDomainsDiffer()
    {
        // Act
        var de = ScanDomain.Resolve("https://www.amazon.de/gp/product/{0}", "arg");
        var nl = ScanDomain.Resolve("https://www.amazon.nl/gp/product/{0}", "arg");

        // Assert
        Assert.NotEqual(de, nl);
    }

    [Fact]
    public void Resolve_GivenAnUpperCasedHost_WhenResolved_ThenTheDomainIsLowerCased()
    {
        // Act
        var domain = ScanDomain.Resolve("https://WWW.Bol.COM/nl/p/{0}", "arg");

        // Assert
        Assert.Equal("bol.com", domain);
    }

    [Fact]
    public void Resolve_GivenTheArgumentCarriesTheUrl_WhenResolved_ThenTheHostComesFromIt()
    {
        // Act
        var domain = ScanDomain.Resolve("{0}", "https://www.coolblue.nl/product/12345");

        // Assert
        Assert.Equal("coolblue.nl", domain);
    }

    [Theory]
    [InlineData("not-a-url-{0}")]      // no scheme at all
    [InlineData("{")]                  // not even a valid format string
    public void Resolve_GivenAnUrlThatIsNotAbsolute_WhenResolved_ThenFallsBackToTheTemplate(string urlTemplate)
    {
        // Act
        var domain = ScanDomain.Resolve(urlTemplate, "arg");

        // Assert
        // The agent keeps a queue of its own rather than sharing one with every other unparseable agent.
        Assert.Equal(urlTemplate, domain);
    }

    [Fact]
    public void Resolve_GivenASource_WhenResolved_ThenUsesItsAgentUrl()
    {
        // Arrange
        var source = new ScanSource(Guid.NewGuid(), "12345",
            new ScanAgent("coolblue", "https://www.coolblue.nl/product/{0}", "pattern", "SimpleRegex", '.'));

        // Act
        var domain = ScanDomain.Resolve(source);

        // Assert
        Assert.Equal("coolblue.nl", domain);
    }
}
