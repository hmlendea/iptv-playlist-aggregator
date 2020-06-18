using System;
using System.Net;
using System.Threading.Tasks;

using IptvPlaylistAggregator.Logging;
using IptvPlaylistAggregator.Service.Models;

using NuciLog.Core;

namespace IptvPlaylistAggregator.Service
{
    public sealed class MediaSourceChecker : IMediaSourceChecker
    {
        readonly IFileDownloader fileDownloader;
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly IDnsResolver dnsResolver;
        readonly ICacheManager cache;
        readonly ILogger logger;

        public MediaSourceChecker(
            IFileDownloader fileDownloader,
            IPlaylistFileBuilder playlistFileBuilder,
            IDnsResolver dnsResolver,
            ICacheManager cache,
            ILogger logger)
        {
            this.fileDownloader = fileDownloader;
            this.playlistFileBuilder = playlistFileBuilder;
            this.dnsResolver = dnsResolver;
            this.cache = cache;
            this.logger = logger;
        }

        public async Task<bool> IsSourcePlayableAsync(string url)
        {
            string resolvedUrl = dnsResolver.ResolveUrl(url);
            string urlToUse = url;
            
            if (string.IsNullOrWhiteSpace(resolvedUrl))
            {
                return false;
            }

            MediaStreamStatus status = cache.GetStreamStatus(url);

            if (!(status is null))
            {
                return status.IsAlive;
            }

            logger.Verbose(MyOperation.MediaSourceCheck, OperationStatus.Started, new LogInfo(MyLogInfoKey.Url, url));

            Uri uri = new Uri(url);
            if (uri.Scheme == "http")
            {
                urlToUse = resolvedUrl;
            }
            
            bool isAlive;

            if (urlToUse.Contains(".m3u") || urlToUse.Contains(".m3u8"))
            {
                isAlive = await IsPlaylistPlayableAsync(urlToUse);
            }
            else if (!urlToUse.EndsWith(".ts"))
            {
                isAlive = await IsStreamPlayableAsync(urlToUse);
            }
            else
            {
                isAlive = false;
            }

            if (isAlive)
            {
                logger.Verbose(MyOperation.MediaSourceCheck, OperationStatus.Success, new LogInfo(MyLogInfoKey.Url, url));
            }
            else
            {
                logger.Verbose(MyOperation.MediaSourceCheck, OperationStatus.Failure, new LogInfo(MyLogInfoKey.Url, url));
            }

            SaveToCache(url, isAlive);
            return isAlive;
        }

        async Task<bool> IsPlaylistPlayableAsync(string url)
        {
            string fileContent = await fileDownloader.TryDownloadStringAsync(url);
            Playlist playlist = playlistFileBuilder.TryParseFile(fileContent);

            return !Playlist.IsNullOrEmpty(playlist);
        }

        async Task<bool> IsStreamPlayableAsync(string url)
        {
            try
            {
                HttpWebRequest request = CreateWebRequest(url);
                HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
                
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        HttpWebRequest CreateWebRequest(string url)
        {
            const int timeout = 3000;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeout;
            request.ContinueTimeout = timeout;
            request.ReadWriteTimeout = timeout;

            return request;
        }

        void SaveToCache(string url, bool isAlive)
        {
            MediaStreamStatus status = new MediaStreamStatus();
            status.Url = url;
            status.IsAlive = isAlive;
            status.LastCheckTime = DateTime.UtcNow;

            cache.StoreStreamStatus(status);
        }
    }
}
