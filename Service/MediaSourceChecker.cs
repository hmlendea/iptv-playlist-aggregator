using System;
using System.Net;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class MediaSourceChecker : IMediaSourceChecker
    {
        readonly IFileDownloader fileDownloader;
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly ICacheManager cache;

        public MediaSourceChecker(
            IFileDownloader fileDownloader,
            IPlaylistFileBuilder playlistFileBuilder,
            ICacheManager cache)
        {
            this.fileDownloader = fileDownloader;
            this.playlistFileBuilder = playlistFileBuilder;
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
                return status.IsAlive;
            }
            
            if (url.Contains(".m3u") || url.Contains(".m3u8"))
            {
                return IsPlaylistPlayable(url);
            }
            
            return IsStreamPlayable(url);
        }

        bool IsPlaylistPlayable(string url)
        {
            Playlist playlist = DownloadPlaylist(url);

            return !Playlist.IsNullOrEmpty(playlist);
        }

        bool IsStreamPlayable(string url)
        {
            try
            {
                UriBuilder uriBuilder = new UriBuilder(url);
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uriBuilder.Uri);
                request.Timeout = 3000;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        Playlist DownloadPlaylist(string url)
        {
            string fileContent = fileDownloader.TryDownloadString(url);
            Playlist playlist = playlistFileBuilder.TryParseFile(fileContent);

            if (!Playlist.IsNullOrEmpty(playlist))
            {
                return playlist;
            }
            
            return null;
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
