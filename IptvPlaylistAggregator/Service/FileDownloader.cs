using System;
using System.Net.Http;
using System.Threading.Tasks;

using NuciWeb.HTTP;

namespace IptvPlaylistAggregator.Service
{
    public sealed class FileDownloader(ICacheManager cache) : IFileDownloader
    {
        public async Task<string> TryDownloadStringAsync(string url)
        {
            string content = cache.GetWebDownload(url);

            if (content is not null)
            {
                return content;
            }

            try
            {
                content = await SendGetRequestAsync(url);
            }
            catch
            {
                return null;
            }

            cache.StoreWebDownload(url, content);

            return content;
        }

        private readonly HttpClient httpClient = CreateHttpClient();

        private async Task<string> SendGetRequestAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            using HttpResponseMessage response = await httpClient.GetAsync(url);
            using HttpContent responseContent = response.Content;

            return await responseContent.ReadAsStringAsync();
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = HttpClientCreator.Create();
            client.Timeout = TimeSpan.FromSeconds(3);

            return client;
        }
    }
}
