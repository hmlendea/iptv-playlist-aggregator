using System;
using System.Net;
using System.Threading.Tasks;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class MediaSourceChecker : IMediaSourceChecker
    {
        readonly IFileDownloader fileDownloader;
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly IDnsResolver dnsResolver;
        readonly ICacheManager cache;

        public MediaSourceChecker(
            IFileDownloader fileDownloader,
            IPlaylistFileBuilder playlistFileBuilder,
            IDnsResolver dnsResolver,
            ICacheManager cache)
        {
            this.fileDownloader = fileDownloader;
            this.playlistFileBuilder = playlistFileBuilder;
            this.dnsResolver = dnsResolver;
            this.cache = cache;
        }

        public async Task<bool> IsSourcePlayableAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            MediaStreamStatus status = cache.GetStreamStatus(url);

            if (!(status is null))
            {
                return status.IsAlive;
            }
            
            bool isAlive;

            if (url.Contains(".m3u") || url.Contains(".m3u8"))
            {
                isAlive = await IsPlaylistPlayableAsync(url);
            }
            else
            {
                isAlive = await IsStreamPlayableAsync(url);
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
            string resolvedUrl = dnsResolver.ResolveUrl(url);

            if (string.IsNullOrWhiteSpace(resolvedUrl))
            {
                return false;
            }

            HttpWebRequest request = CreateWebRequest(resolvedUrl);

            try
            {
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
            const int timeout = 7500;

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
