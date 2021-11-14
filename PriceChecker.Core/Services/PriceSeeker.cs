using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Genius.Atom.Infrastructure.Io;
using Genius.Atom.Infrastructure.Net;
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
    private readonly IFileService _io;
    private readonly ILogger<PriceSeeker> _logger;

    private const char DEFAULT_DECIMAL_DELIMITER = '.';

    private static readonly object _locker = new();

    public PriceSeeker(ITrickyHttpClient trickyHttpClient, IFileService io, ILogger<PriceSeeker> logger)
    {
        _trickyHttpClient = trickyHttpClient;
        _io = io;
        _logger = logger;
    }

    public async Task<PriceSeekResult[]> SeekAsync(Product product, CancellationToken cancel)
    {
        var result = product.Sources.AsParallel().Select(async (productSource) =>
            await Seek(productSource, cancel));

        return await Task.WhenAll(result)
            .ContinueWith(t => t.Result?
                    .Where(x => x != null)
                    .Select(x => x!.Value)
                    .ToArray() ?? new PriceSeekResult[0],
                TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private async Task<PriceSeekResult?> Seek(ProductSource productSource, CancellationToken cancel)
    {
        var agent = productSource.Agent;
        var url = string.Format(agent.Url, productSource.AgentArgument);
        string? content;
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
            return null;

        var re = new Regex(agent.PricePattern);
        var match = re.Match(content);
        if (!match.Success)
        {
            var dumpFileName = $"dump ({productSource.Id}).log";
            lock(_locker)
            {
                _io.WriteTextToFile(dumpFileName, content);
            }
            _logger.LogError("Cannot match price from the given content. File = '{dumpFileName}', Url = '{url}'", dumpFileName, url);
            return null;
        }

        if (!TryParsePrice(match, out var price))
            return null;

        if (price <= 0.0m)
        {
            _logger.LogWarning("Price for product '{productSourceAgentArgument}' at '{agentId}' is invalid: {price}",
                productSource.AgentArgument, agent.Id, price);
            return null;
        }

        return new PriceSeekResult(productSource.Id, agent.Key, price);


        bool TryParsePrice(Match match, out decimal price)
        {
            var priceString = match.Groups["price"].Value;
            if (agent.DecimalDelimiter != DEFAULT_DECIMAL_DELIMITER)
                priceString = priceString.Replace(agent.DecimalDelimiter, DEFAULT_DECIMAL_DELIMITER);

            var priceConverted = decimal.TryParse(priceString, out price);
            if (!priceConverted)
            {
                _logger.LogError("Could not convert the price '{priceString}' to decimal. Url = '{url}'", priceString, url);
                return false;
            }

            return true;
        }
    }
}
