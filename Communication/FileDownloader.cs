using System;
using System.Net;

namespace IptvPlaylistFetcher.Communication
{
    public sealed class FileDownloader : WebClient
    {
        const int DefaultTimeout = 1000;

        public int Timeout { get; set; }

        public FileDownloader()
        {
            Timeout = DefaultTimeout;
        }

        public FileDownloader(int timeoutMillis)
        {
            Timeout = timeoutMillis;
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest request = base.GetWebRequest(uri);
            request.Timeout = Timeout;

            return request;
        }
    }
}
