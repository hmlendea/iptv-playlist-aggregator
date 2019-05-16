using System;
using System.Net;

namespace IptvPlaylistAggregator.Communication
{
    public sealed class FileDownloader : WebClient, IFileDownloader
    {
        const int DefaultTimeout = 5000;

        public int Timeout { get; set; }

        public FileDownloader()
        {
            Timeout = DefaultTimeout;
        }

        public FileDownloader(int timeoutMillis)
        {
            Timeout = timeoutMillis;
        }

        public string DownloadString(string url)
            => base.DownloadString(url);

        public string TryDownloadString(string url)
        {
            try
            {
                return DownloadString(url);
            }
            catch
            {
                return null;
            }
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest request = base.GetWebRequest(uri);
            request.Timeout = Timeout;

            return request;
        }
    }
}
