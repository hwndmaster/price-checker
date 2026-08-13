using System.Globalization;
using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.Net;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Services;

public interface IPriceSeeker
{
    Task<PriceSeekResult[]> SeekAsync(ScanProduct product, CancellationToken cancel);
}

internal sealed class PriceSeeker : IPriceSeeker
{
    private readonly ITrickyHttpClient _trickyHttpClient;
    private readonly IAgentHandlersProvider _agentHandlersProvider;
    private readonly IFileService _io;
    private readonly ILogger<PriceSeeker> _logger;

    private static readonly Lock Locker = new();

    public PriceSeeker(ITrickyHttpClient trickyHttpClient, IFileService io,
        IAgentHandlersProvider agentHandlersProvider, ILogger<PriceSeeker> logger)
    {
        _trickyHttpClient = trickyHttpClient.NotNull();
        _io = io.NotNull();
        _agentHandlersProvider = agentHandlersProvider.NotNull();
        _logger = logger.NotNull();
    }

    public async Task<PriceSeekResult[]> SeekAsync(ScanProduct product, CancellationToken cancel)
    {
        Guard.NotNull(product);

        var result = product.Sources.AsParallel().Select(async (productSource) =>
            await SeekAsync(productSource, cancel).ConfigureAwait(false));

        return await Task.WhenAll(result).ConfigureAwait(false);
    }

    private async Task<PriceSeekResult> SeekAsync(ScanSource productSource, CancellationToken cancel)
    {
        var agent = productSource.Agent;
        var url = string.Format(CultureInfo.InvariantCulture, agent.Url, productSource.Argument);
        string? content;
        var resultTemplate = new PriceSeekResult(AgentHandlingStatus.Success, productSource.SourceId, agent.Key, null);

        try
        {
            content = await _trickyHttpClient.DownloadContentAsync(url, cancel).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed loading content for source `{ProductSourceAgentKey}`, url = `{Url}`", agent.Key, url);
            throw;
        }
        if (content is null)
            return resultTemplate with { Status = AgentHandlingStatus.CouldNotFetch };

        var handler = _agentHandlersProvider.FindByName(agent.Handler)
            ?? throw new InvalidOperationException($"Handler `{agent.Handler}` not found");

        var result = handler.Handle(agent, content, out var price);

        if (result == AgentHandlingStatus.CouldNotMatch)
        {
            var dumpFileName = $"dump ({productSource.SourceId}).log";
            lock (Locker)
            {
                _io.WriteTextToFile(dumpFileName, content);
            }
            _logger.LogError("Cannot match price from the given content. File = '{DumpFileName}', Url = '{Url}'", dumpFileName, url);
            return resultTemplate with { Status = result };
        }
        else if (result == AgentHandlingStatus.CouldNotParse)
        {
            return resultTemplate with { Status = result };
        }
        else if (result == AgentHandlingStatus.InvalidPrice)
        {
            _logger.LogError("Invalid price from the given content. Url = '{Url}', Argument = {Argument}, Agent = {Agent}, Price = {Price}", url, productSource.Argument, agent.Key, price);
            return resultTemplate with { Status = result };
        }

        return resultTemplate with { Status = result, Price = price };
    }
}
