using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Genius.PriceChecker.Core.Messages;
using Genius.PriceChecker.Core.Repositories;
using Genius.PriceChecker.Core.Models;
using System.IO;

namespace Genius.PriceChecker.Core.Services
{
    public interface IPriceSeeker
    {
        Task<IEnumerable<PriceSeekResult>> SeekAsync(Product product);
    }

    internal sealed class PriceSeeker : IPriceSeeker
    {
        private readonly IAgentRepository _sourceRepo;
        private readonly ILogger<PriceSeeker> _logger;

        private const char DEFAULT_DECIMAL_DELIMITER = '.';

        public PriceSeeker(IAgentRepository sourceRepo, ILogger<PriceSeeker> logger)
        {
            _sourceRepo = sourceRepo;
            _logger = logger;
        }

        public async Task<IEnumerable<PriceSeekResult>> SeekAsync(Product product)
        {
            var result = new List<PriceSeekResult>();
            var sources = _sourceRepo.GetAll();

            foreach (var productSource in product.Sources)
            {
                var source = sources.FirstOrDefault(x => x.Id == productSource.AgentId);
                if (source == null)
                {
                    _logger.LogError($"Source not found: {productSource.AgentId}");
                    continue;
                }
                var resultPerProductSource = await Seek(productSource.AgentArgument, source);
                if (resultPerProductSource != null)
                {
                    result.Add(resultPerProductSource);
                }
            }

            return result;
        }

        private async Task<PriceSeekResult> Seek(string productId, Agent agent)
        {
            var url = string.Format(agent.Url, productId);
            var httpClient = new HttpClient();

            // Needed for tweakers.net:
            httpClient.DefaultRequestHeaders.Add("X-Cookies-Accepted", "1");
            httpClient.DefaultRequestHeaders.Add("accept", "text/html");
            httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                // Something went wrong
                _logger.LogError($"Failed to fetch '{url}'. Error Code = {response.StatusCode}");
                return null;
            }
            var content = await response.Content.ReadAsStringAsync();

            var re = new Regex(agent.PricePattern);
            var match = re.Match(content);
            if (!match.Success)
            {
                await File.WriteAllTextAsync("content.log", content);
                _logger.LogError($"Cannot match price from the given content. File = 'content.log', Url = '{url}'");
                return null;
            }

            if (!TryParsePrice(match, out var price))
            {
                return null;
            }

            if (price <= 0.0m)
            {
                _logger.LogWarning($"Price for product '{productId}' at '{agent.Id}' is invalid: {price}");
                return null;
            }

            return new PriceSeekResult { AgentId = agent.Id, ProductId = productId, Price = price };


            bool TryParsePrice(Match match, out decimal price)
            {
                var priceString = match.Groups["price"].Value;
                if (agent.DecimalDelimiter != DEFAULT_DECIMAL_DELIMITER)
                {
                    priceString = priceString.Replace(agent.DecimalDelimiter, DEFAULT_DECIMAL_DELIMITER);
                }
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
