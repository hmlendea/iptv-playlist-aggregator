using System;
using System.Net;

namespace IptvPlaylistAggregator.Service
{
    public sealed class FileDownloader : WebClient, IFileDownloader
    {
        const int DefaultTimeout = 5000;

        public int Timeout { get; set; }

        readonly ICacheManager cache;

        public FileDownloader(ICacheManager cache)
        {
            Timeout = DefaultTimeout;

            this.cache = cache;
        }

        public FileDownloader(int timeoutMillis)
        {
            Timeout = timeoutMillis;
        }
        
        public string TryDownloadString(string url)
        {
            string content = cache.GetWebDownload(url);

            if (!string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            try
            {
                content = DownloadString(url);
            }
            catch
            {
                content = null;
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
