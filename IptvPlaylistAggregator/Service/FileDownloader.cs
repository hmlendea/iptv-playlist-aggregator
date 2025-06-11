using System;
using System.Net.Http;
using System.Threading.Tasks;
using NuciWeb.HTTP;

namespace IptvPlaylistAggregator.Service
{
    public sealed class FileDownloader : IFileDownloader
    {
        private readonly ICacheManager cache;
        private readonly HttpClient httpClient;

        public FileDownloader(ICacheManager cache)
        {
            this.cache = cache;

            httpClient = HttpClientCreator.Create();
            httpClient.Timeout = TimeSpan.FromSeconds(3);
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
