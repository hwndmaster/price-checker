using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Genius.PriceChecker.Core.Services
{
    public interface ITrickyHttpClient
    {
        Task<string> DownloadContent(string url);
    }

    public class TrickyHttpClient : ITrickyHttpClient
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _lockers = new();
        private readonly ILogger<TrickyHttpClient> _logger;
        private static object _locker = new object();

        private const int DELAY_MS = 500;

        public TrickyHttpClient(ILogger<TrickyHttpClient> logger)
        {
            _logger = logger;
        }

        public async Task<string> DownloadContent(string url)
        {
            var uri = new Uri(url);

            var locker = _lockers.GetOrAdd(uri.Host, (_) => new SemaphoreSlim(1));
            await locker.WaitAsync();
            try
            {
                await Task.Delay(DELAY_MS);
                return await DownloadInternal(url);
            }
            finally
            {
                locker.Release();
            }
        }

        private async Task<string> DownloadInternal(string url)
        {
            var httpClient = new HttpClient();

            // To confuse the hosts
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
            return await response.Content.ReadAsStringAsync();
        }
    }
}