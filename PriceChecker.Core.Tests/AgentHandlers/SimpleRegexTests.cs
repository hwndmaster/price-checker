using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Tests.AgentHandlers;

public class SimpleRegexTests
{
    private readonly Fixture _fixture = new();

    private readonly SimpleRegex _sut;

    public SimpleRegexTests()
    {
        _sut = new(Mock.Of<ILogger<SimpleRegex>>());
    }

    [Fact]
    public void Handle__Happy_flow_scenario()
    {
        // Arrange
        var (agent, content, price) = CreateSampleAgentAndContent();

        // Act
        var result = _sut.Handle(agent, content, out var actualPrice);

        // Verify
        Assert.Equal(AgentHandlingStatus.Success, result);
        Assert.Equal(actualPrice, price);
    }

    [Fact]
    public void Handle__DecimalDelimiter_is_not_default__Considered_in_price_parse()
    {
        // Arrange
        var (agent, content, price) = CreateSampleAgentAndContent(delimiter: ';');

        // Act
        var result = _sut.Handle(agent, content, out var actualPrice);

        // Verify
        Assert.Equal(AgentHandlingStatus.Success, result);
        Assert.Equal(actualPrice, price);
    }

    [Fact]
    public void Handle__Price_is_invalid__Returns_null()
    {
        // Arrange
        const decimal priceInvalid = 0.0m;
        var (agent, content, _) = CreateSampleAgentAndContent(price: priceInvalid);

        // Act
        var result = _sut.Handle(agent, content, out var actualPrice);

        // Verify
        Assert.Equal(AgentHandlingStatus.InvalidPrice, result);
        Assert.Null(actualPrice);
    }

    private (Agent Agent, string Content, decimal price) CreateSampleAgentAndContent(
        char delimiter = '.',
        decimal? price = null)
    {
        var agent = _fixture.Build<Agent>()
            .With(x => x.PricePattern, $@"`(?<price>[\d\{delimiter}]+)`")
            .With(x => x.DecimalDelimiter, delimiter)
            .Create();

        if (price is not null)
        {
            return (agent, $"`{price!.Value}`", price.Value);
        }

        var priceDec = _fixture.Create<int>();
        var priceFlt = _fixture.Create<int>() % 99;
        var content = $"{_fixture.Create<string>()}`{priceDec}{delimiter}{priceFlt:00}`{_fixture.Create<string>()}";
        return (agent, content, priceDec + priceFlt / 100m);
    }
}
