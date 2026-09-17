using Genius.PriceChecker.Core.Services;

namespace Genius.PriceChecker.Core.Tests.Services;

public sealed class SourceUrlTests
{
    [Fact]
    public void Resolve_GivenATemplateAndAnArgument_ThenTheArgumentIsFilledIn()
    {
        // Act
        var url = SourceUrl.Resolve("https://example.com/p/{0}?full=1", "B000123");

        // Assert
        Assert.Equal("https://example.com/p/B000123?full=1", url);
    }

    [Fact]
    public void Resolve_GivenATemplateWithoutAPlaceholder_ThenTheTemplateIsReturnedUnchanged()
    {
        // Act
        var url = SourceUrl.Resolve("https://example.com/the-one-product", "ignored");

        // Assert
        Assert.Equal("https://example.com/the-one-product", url);
    }

    [Theory]
    [InlineData("{")]                       // not a valid format string at all
    [InlineData("https://example.com/{1}")] // an argument the template asks for but is not given
    public void Resolve_GivenATemplateTheArgumentCannotBeAppliedTo_ThenFallsBackToTheTemplate(string urlTemplate)
    {
        // Act
        var url = SourceUrl.Resolve(urlTemplate, "B000123");

        // Assert: a misconfigured agent must not fail everything that asks for a link.
        Assert.Equal(urlTemplate, url);
    }
}
