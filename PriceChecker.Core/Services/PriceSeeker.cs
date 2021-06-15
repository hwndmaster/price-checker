using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Services
{
  public interface IPriceSeeker
    {
        Task<PriceSeekResult[]> SeekAsync(Product product);
    }

    internal sealed class PriceSeeker : IPriceSeeker
    {
        private readonly IAgentRepository _agentRepo;
        private readonly ITrickyHttpClient _trickyHttpClient;
        private readonly ILogger<PriceSeeker> _logger;

        private const char DEFAULT_DECIMAL_DELIMITER = '.';

        private static object _locker = new();

        public PriceSeeker(IAgentRepository agentRepo, ITrickyHttpClient trickyHttpClient,
            ILogger<PriceSeeker> logger)
        {
            _agentRepo = agentRepo;
            _trickyHttpClient = trickyHttpClient;
            _logger = logger;
        }

        public async Task<PriceSeekResult[]> SeekAsync(Product product)
        {
            var result = product.Sources.AsParallel().Select(async (productSource) => {
                return await Seek(productSource);
            });

            return await Task.WhenAll(result)
                .ContinueWith(x => x.Result?.Where(x => x != null).ToArray() ?? new PriceSeekResult[0]);
        }

        private async Task<PriceSeekResult> Seek(ProductSource productSource)
        {
            var agent = _agentRepo.FindById(productSource.AgentId);
            if (agent == null)
            {
                _logger.LogError($"Source not found: {productSource.AgentId}");
                return null;
            }

            var url = string.Format(agent.Url, productSource.AgentArgument);
            var content = await _trickyHttpClient.DownloadContent(url);
            if (content == null)
                return null;

            var re = new Regex(agent.PricePattern);
            var match = re.Match(content);
            if (!match.Success)
            {
                lock(_locker)
                {
                    File.WriteAllText($"dump ({productSource.Id}).log", content, Encoding.UTF8);
                }
                _logger.LogError($"Cannot match price from the given content. File = 'content.log', Url = '{url}'");
                return null;
            }

            if (!TryParsePrice(match, out var price))
                return null;

            if (price <= 0.0m)
            {
                _logger.LogWarning($"Price for product '{productSource.AgentArgument}' at '{agent.Id}' is invalid: {price}");
                return null;
            }

            return new PriceSeekResult {
                ProductSourceId = productSource.Id,
                AgentId = agent.Id,
                Price = price
            };


            bool TryParsePrice(Match match, out decimal price)
            {
                var priceString = match.Groups["price"].Value;
                if (agent.DecimalDelimiter != DEFAULT_DECIMAL_DELIMITER)
                    priceString = priceString.Replace(agent.DecimalDelimiter, DEFAULT_DECIMAL_DELIMITER);

                var priceConverted = decimal.TryParse(priceString, out price);
                if (!priceConverted)
                {
                    _logger.LogError($"Could not convert the price '{priceString}' to decimal. Url = '{url}'");
                    return false;
                }

                return true;
            }
        }
    }
}
