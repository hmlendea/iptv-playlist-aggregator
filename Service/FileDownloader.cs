using System;
using System.Net.Http;
using System.Threading.Tasks;

using IptvPlaylistAggregator.Configuration;

namespace IptvPlaylistAggregator.Service
{
    public sealed class FileDownloader : IFileDownloader
    {
        readonly IDnsResolver dnsResolver;
        readonly ICacheManager cache;
        readonly ApplicationSettings applicationSettings;

        readonly HttpClient httpClient;

        public FileDownloader(
            IDnsResolver dnsResolver,
            ICacheManager cache,
            ApplicationSettings applicationSettings)
        {
            this.dnsResolver = dnsResolver;
            this.cache = cache;
            this.applicationSettings = applicationSettings;

            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMilliseconds(3000);
            httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                applicationSettings.UserAgent);
        }

        public async Task<string> TryDownloadStringAsync(string url)
        {
            string content = cache.GetWebDownload(url);

            if (!(content is null))
            {
                return content;
            }

            string resolvedUrl = dnsResolver.ResolveUrl(url);

            if (!(resolvedUrl is null))
            {
                try
                {
                    content = await GetAsync(url);
                }
                catch { }
            }

            cache.StoreWebDownload(url, content);
            return content;
        }

        async Task<string> GetAsync(string url)
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(url))
            {
                using (HttpContent content = response.Content)
                {
                    return await content.ReadAsStringAsync();
                }
            }
        }
    }
}
