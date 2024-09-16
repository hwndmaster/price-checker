using Genius.PriceChecker.Core.Models;

namespace Genius.PriceChecker.Core.Tests;

public static class ModelHelpers
{
    private static readonly Fixture _fixture = new();

    public static Product SampleProduct(ICollection<Agent>? agents = null)
    {
        var product = _fixture.Create<Product>();
        var agentsStack = new Stack<Agent>(agents ?? Array.Empty<Agent>());
        if (agents is not null)
        {
            agentsStack = new Stack<Agent>(agentsStack.RandomizeOrder());
        }
        foreach (var (first, second) in product.Recent.Zip(product.Sources))
        {
            first.ProductSourceId = second.Id;
            if (agents is null)
            {
                second.Agent = _fixture.Build<Agent>()
                    .With(x => x.Key, second.AgentKey)
                    .Create();
            }
            else
            {
                second.Agent = agentsStack.Pop();
                second.AgentKey = second.Agent.Key;
            }
        }
        return product;
    }

    public static IEnumerable<T> RandomizeOrder<T>(this IEnumerable<T> source)
    {
        return source.OrderBy(_ => Guid.NewGuid());
    }

    public static IEnumerable<Product> SampleManyProducts()
    {
        return Enumerable.Range(1, 3).Select(_ => SampleProduct());
    }

    public static IEnumerable<Agent> SampleManyAgents(IEnumerable<Product> products)
    {
        return products.SelectMany(x => x.Sources).Select(x => x.Agent);
    }

    public static Agent[] Clone(Agent[] agents)
    {
        return agents.Select(x => (Agent)x.Clone()).ToArray();
    }
}
