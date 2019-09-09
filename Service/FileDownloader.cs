using System;
using System.Net;
using System.Threading.Tasks;

namespace IptvPlaylistAggregator.Service
{
    public sealed class FileDownloader : WebClient, IFileDownloader
    {
        const int DefaultTimeout = 5000;

        public int Timeout { get; set; }

        readonly IDnsResolver dnsResolver;
        readonly ICacheManager cache;

        public FileDownloader(
            IDnsResolver dnsResolver,
            ICacheManager cache)
        {
            Timeout = DefaultTimeout;

            this.dnsResolver = dnsResolver;
            this.cache = cache;
        }

        public FileDownloader(int timeoutMillis)
        {
            Timeout = timeoutMillis;
        }
        
        public string TryDownloadString(string url)
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
                    content = DownloadString(resolvedUrl);
                }
                catch { }
            }

            cache.StoreWebDownload(url, content);
            return content;
        }
        
        public async Task<string> TryDownloadStringTaskAsync(string url)
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
                    content = await DownloadStringTaskAsync(resolvedUrl);
                }
                catch { }
            }

            cache.StoreWebDownload(url, content);
            return content;
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest request = base.GetWebRequest(uri);
            request.Timeout = Timeout;

            return request;
        }
    }
}
