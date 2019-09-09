using System.Net.Http;
using System.Threading.Tasks;

namespace IptvPlaylistAggregator.Service
{
    public sealed class FileDownloader : IFileDownloader
    {
        readonly IDnsResolver dnsResolver;
        readonly ICacheManager cache;

        readonly HttpClient httpClient;

        public FileDownloader(
            IDnsResolver dnsResolver,
            ICacheManager cache)
        {
            this.dnsResolver = dnsResolver;
            this.cache = cache;

            httpClient = new HttpClient();
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
