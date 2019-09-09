using System;
using System.Net;

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

        public bool IsSourcePlayable(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            MediaStreamStatus status = cache.GetStreamStatus(url);

            if (!(status is null))
            {
                Console.WriteLine("cache hit");
                return status.IsAlive;
            }
            
            bool isAlive;

            if (url.Contains(".m3u") || url.Contains(".m3u8"))
            {
                isAlive = IsPlaylistPlayable(url);
            }
            else
            {
                isAlive = IsStreamPlayable(url);
            }

            SaveToCache(url, isAlive);
            return isAlive;
        }

        bool IsPlaylistPlayable(string url)
        {
            Playlist playlist = DownloadPlaylist(url);
            return !Playlist.IsNullOrEmpty(playlist);
        }

        bool IsStreamPlayable(string url)
        {
            string resolvedUrl = dnsResolver.ResolveUrl(url);

            if (string.IsNullOrWhiteSpace(resolvedUrl))
            {
                return false;
            }

            try
            {
                Uri uri = new Uri(resolvedUrl);
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
                request.Method = "HEAD";
                request.Timeout = 4000;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(resolvedUrl + " (" + url + ")" + Environment.NewLine + ex.Message);
            }

            return false;
        }

        Playlist DownloadPlaylist(string url)
        {
            string fileContent = fileDownloader.TryDownloadString(url);
            Playlist playlist = playlistFileBuilder.TryParseFile(fileContent);

            return playlist;
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
