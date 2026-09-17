using System.Globalization;
using System.Text;
using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.Net;
using Genius.PriceChecker.Core.AgentHandlers;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Dto;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Services;

public interface IPriceSeeker
{
    /// <summary>
    ///   Fetches and parses the price of a single product source. Seeking one source at a time is what
    ///   lets <see cref="IScanSessionRunner"/> spread the sources of a session over their domains; the
    ///   failures that are not a parsing outcome, such as an unreachable host, are reported by throwing.
    /// </summary>
    Task<PriceSeekResult> SeekAsync(ScanSource productSource, CancellationToken cancel);
}

internal sealed class PriceSeeker : IPriceSeeker
{
    private readonly ITrickyHttpClient _trickyHttpClient;
    private readonly IAgentHandlersProvider _agentHandlersProvider;
    private readonly IFileService _io;
    private readonly ILogger<PriceSeeker> _logger;

    private static readonly SemaphoreSlim DumpLock = new(1, 1);

    public PriceSeeker(ITrickyHttpClient trickyHttpClient, IFileService io,
        IAgentHandlersProvider agentHandlersProvider, ILogger<PriceSeeker> logger)
    {
        _trickyHttpClient = trickyHttpClient.NotNull();
        _io = io.NotNull();
        _agentHandlersProvider = agentHandlersProvider.NotNull();
        _logger = logger.NotNull();
    }

    public async Task<PriceSeekResult> SeekAsync(ScanSource productSource, CancellationToken cancel)
    {
        Guard.NotNull(productSource);

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
            var dumpFilePath = Path.Combine("Logs", $"dump ({productSource.SourceId}).log");
            await DumpLock.WaitAsync(cancel).ConfigureAwait(false);
            try
            {
                await _io.WriteTextToFileAsync(dumpFilePath, content, Encoding.UTF8, cancel).ConfigureAwait(false);
            }
            finally
            {
                DumpLock.Release();
            }
            _logger.LogError("Cannot match price from the given content. File = '{DumpFilePath}', Url = '{Url}'", dumpFilePath, url);
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
