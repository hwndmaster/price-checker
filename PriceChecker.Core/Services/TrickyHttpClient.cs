using System;
using System.Collections.Concurrent;
using System.Net;
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
        private static object _locker = new();

        private const int DELAY_MS = 500;
        private const int MAX_REPEATS = 5;

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
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            for (var irepeat = 1; irepeat <= MAX_REPEATS; irepeat++)
            {
                var httpClient = new HttpClient(handler);

                // To confuse the hosts
                httpClient.DefaultRequestHeaders.Add("X-Cookies-Accepted", "1");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                httpClient.DefaultRequestHeaders.Add("User-Agent", CreateRandomUserAgent());

                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        await Task.Delay(DELAY_MS * (irepeat + 1));
                        continue;
                    }

                    // Something went wrong
                    _logger.LogError($"Failed to fetch '{url}'. Error Code = {response.StatusCode}");
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }

            return null;
        }

        private string CreateRandomUserAgent()
        {
            var platform = new [] { "Machintosh", "Windows", "X11" }.TakeRandom();
            var os = (platform switch {
                "Machintosh" => new [] { "68K", "PPC" },
                "Windows" => new [] { "Win3.11", "WinNT3.51", "WinNT4.0", "Windows NT 5.0", "Windows NT 5.1", "Windows NT 5.2", "Windows NT 6.0", "Windows NT 6.1", "Windows NT 6.2", "Win95", "Win98", "Win 9x 4.90", "WindowsCE" },
                "X11" => new [] { "Linux i686", "Linux x86_64" },
                _ => new string[] {}
            }).TakeRandom();
            var browser = new [] { "Chrome", "Firefox", "IE" }.TakeRandom();

            if (browser == "Chrome")
            {
			    var webkit = Utils.RandomInt(500, 599).ToString();
			    var version = $"{Utils.RandomInt(0, 24)}.0{Utils.RandomInt(0, 1500)}.{Utils.RandomInt(0, 999)}";

			    return $"Mozilla/5.0 ({os}) AppleWebKit{webkit}.0 (KHTML, live Gecko) Chrome/{version} Safari/{webkit}";
            }
            if (browser == "Firefox")
            {
                var year = Utils.RandomInt(2000, 2021);
                var month = Utils.RandomInt(1, 12);
                var day = Utils.RandomInt(1, 28);
                var gecko = $"{year}{month:00}{day:00}";
                var version = $"{Utils.RandomInt(1, 15)}.0";

                return $"Mozillia/5.0 ({os}; rv:{version}) Gecko/{gecko} Firefox/{version}";
            }
            if (browser == "IE")
            {
                var version = $"{Utils.RandomInt(1, 10)}.0";
                var engine = $"{Utils.RandomInt(1, 5)}.0";
                var option = Utils.RandomBool();
                string token;
                if (option)
                {
                    var v = new [] { ".NET CLR", "SV1", "Tablet PC", "Win64; IA64", "Win64; x64", "WOW64" }.TakeRandom();
                    token = $"{v};";
                }
                else
                    token = "";

                return $"Mozilla/5.0 (compatible; MSIE {version}; {os}; {token}Trident/{engine})";
            }

            throw new NotImplementedException();
        }
    }
}
