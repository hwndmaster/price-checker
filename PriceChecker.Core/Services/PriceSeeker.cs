using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.Net;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Services;

public interface IPriceSeeker
{
    Task<PriceSeekResult[]> SeekAsync(Product product, CancellationToken cancel);
}

internal sealed class PriceSeeker : IPriceSeeker
{
    private readonly ITrickyHttpClient _trickyHttpClient;
    private readonly IAgentHandlersProvider _agentHandlersProvider;
    private readonly IFileService _io;
    private readonly ILogger<PriceSeeker> _logger;

    private static readonly object _locker = new();

    public PriceSeeker(ITrickyHttpClient trickyHttpClient, IFileService io,
        IAgentHandlersProvider agentHandlersProvider, ILogger<PriceSeeker> logger)
    {
        _trickyHttpClient = trickyHttpClient;
        _io = io;
        _agentHandlersProvider = agentHandlersProvider;
        _logger = logger;
    }

    public async Task<PriceSeekResult[]> SeekAsync(Product product, CancellationToken cancel)
    {
        var result = product.Sources.AsParallel().Select(async (productSource) =>
            await Seek(productSource, cancel));

        return await Task.WhenAll(result);
    }

    private async Task<PriceSeekResult> Seek(ProductSource productSource, CancellationToken cancel)
    {
        var agent = productSource.Agent;
        var url = string.Format(agent.Url, productSource.AgentArgument);
        string? content;
        var resultTemplate = new PriceSeekResult(AgentHandlingStatus.Success, productSource.Id, agent.Key, null);

        try
        {
            content = await _trickyHttpClient.DownloadContent(url, cancel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed loading content for source `{productSourceAgentKey}`, url = `{url}`", productSource.AgentKey, url);
            throw;
        }
        if (content is null)
            return resultTemplate with { Status = AgentHandlingStatus.CouldNotFetch };

        var handler = _agentHandlersProvider.FindByName(agent.Handler)
            ?? throw new Exception($"Handler `{agent.Handler}` not found");

        var result = handler.Handle(agent, content, out var price);

        if (result == AgentHandlingStatus.CouldNotMatch)
        {
            var dumpFileName = $"dump ({productSource.Id}).log";
            lock(_locker)
            {
                _io.WriteTextToFile(dumpFileName, content);
            }
            _logger.LogError("Cannot match price from the given content. File = '{dumpFileName}', Url = '{url}'", dumpFileName, url);
            return resultTemplate with { Status = result };
        }
        else if (result == AgentHandlingStatus.CouldNotParse)
        {
            return resultTemplate with { Status = result };
        }
        else if (result == AgentHandlingStatus.InvalidPrice)
        {
            _logger.LogError("Invalid price from the given content. Url = '{url}', Product = {product}, Agent = {agent}, Price = {price}", url, productSource.AgentArgument, agent.Key, price);
            return resultTemplate with { Status = result };
        }

        return resultTemplate with { Status = result, Price = price };
    }
}
