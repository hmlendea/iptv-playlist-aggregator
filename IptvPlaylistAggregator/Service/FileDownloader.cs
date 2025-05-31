using System;
using System.Net.Http;
using System.Threading.Tasks;

using IptvPlaylistAggregator.Configuration;

namespace IptvPlaylistAggregator.Service
{
    public sealed class FileDownloader : IFileDownloader
    {
        private readonly ICacheManager cache;
        private readonly HttpClient httpClient;

        public FileDownloader(
            ICacheManager cache,
            ApplicationSettings applicationSettings)
        {
            this.cache = cache;

            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(3000)
            };

            httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                applicationSettings.UserAgent);
        }

        public async Task<string> TryDownloadStringAsync(string url)
        {
            string content = cache.GetWebDownload(url);

            if (content is not null)
            {
                return content;
            }

            try
            {
                content = await GetAsync(url);
            }
            catch
            {
                return null;
            }

            cache.StoreWebDownload(url, content);

            return content;
        }

        private async Task<string> GetAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            string content = await SendGetRequestAsync(url);
            cache.StoreWebDownload(url, content);

            return content;
        }

        private async Task<string> SendGetRequestAsync(string url)
        {
            using HttpResponseMessage response = await httpClient.GetAsync(url);
            using HttpContent content = response.Content;

            return await content.ReadAsStringAsync();
        }
    }
}
