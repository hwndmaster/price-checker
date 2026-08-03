using System.Text;
using Genius.Atom.Data.JsonPersistence;
using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.Infrastructure.TestingUtil.Io;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Tests.Persistence;

public sealed class GuidReferenceJsonConverterTests
{
    private const string SampleAgentId = "6f35fbc4-4e3b-4734-a32e-66bdcc904d15";

    private readonly FakeFileService _fileService = new();
    private readonly IJsonPersister _persister;

    public GuidReferenceJsonConverterTests()
    {
        var services = new ServiceCollection();
        Atom.Data.Module.Configure(services);
        services.AddSingleton<IFileService>(_fileService);
        services.AddSingleton(typeof(ILogger<>), typeof(FakeLogger<>));
        services.AddSingleton<IJsonConverter, GuidReferenceJsonConverter<AgentRef>>();
        services.AddSingleton<IJsonConverter, GuidReferenceJsonConverter<ProductRef>>();

        _persister = services.BuildServiceProvider().GetRequiredService<IJsonPersister>();
    }

    [Fact]
    public void LoadCollection__Reads_legacy_data_file_with_plain_guid_ids()
    {
        // Arrange
        const string file = @".\Data\Agent.json";
        _fileService.AddFile(file, Encoding.UTF8.GetBytes($$"""
            [
              {
                "Key": "amazon.de",
                "Url": "https://www.amazon.de/gp/product/{0}?psc=1",
                "PricePattern": "some-pattern",
                "Handler": "SimpleRegex",
                "DecimalDelimiter": ".",
                "Id": "{{SampleAgentId}}"
              }
            ]
            """));

        // Act
        var result = _persister.LoadCollection<Agent>(file);

        // Verify
        Assert.NotNull(result);
        var agent = Assert.Single(result);
        Assert.Equal(new Guid(SampleAgentId), agent.Id.Id);
        Assert.Equal("amazon.de", agent.Key);
        Assert.Equal('.', agent.DecimalDelimiter);
    }

    [Fact]
    public void Store_and_LoadCollection__Round_trips_references_as_plain_guid_strings()
    {
        // Arrange
        const string file = @".\Data\Agent.json";
        var agent = new Agent
        {
            Id = AgentRef.Create(new Guid(SampleAgentId)),
            Key = "amazon.de",
            Url = "https://www.amazon.de/gp/product/{0}?psc=1",
            PricePattern = "some-pattern",
            Handler = "SimpleRegex",
            DecimalDelimiter = ','
        };

        // Act
        _persister.Store(file, new[] { agent });
        var result = _persister.LoadCollection<Agent>(file);

        // Verify
        Assert.Contains($"\"{SampleAgentId}\"", _fileService.ReadTextFromFile(file));
        Assert.NotNull(result);
        Assert.Equal(agent, Assert.Single(result));
    }
}
