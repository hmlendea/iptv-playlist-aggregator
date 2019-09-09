using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using NuciLog.Core;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Logging;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class PlaylistFetcher : IPlaylistFetcher
    {
        readonly IFileDownloader fileDownloader;        
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly ICacheManager cache;
        readonly ApplicationSettings applicationSettings;
        readonly ILogger logger;

        public PlaylistFetcher(
            IFileDownloader fileDownloader,
            IPlaylistFileBuilder playlistFileBuilder,
            ICacheManager cache,
            ApplicationSettings applicationSettings,
            ILogger logger)
        {
            this.fileDownloader = fileDownloader;
            this.playlistFileBuilder = playlistFileBuilder;
            this.applicationSettings = applicationSettings;
            this.cache = cache;
            this.logger = logger;
        }

        public IEnumerable<Playlist> FetchProviderPlaylists(IEnumerable<PlaylistProvider> providers)
        {
            ConcurrentDictionary<int, Playlist> playlists = new ConcurrentDictionary<int, Playlist>();

            logger.Info(MyOperation.PlaylistFetching, OperationStatus.Started, "Fetching provider playlists");

            foreach (PlaylistProvider provider in providers)
            {
                Playlist playlist = FetchProviderPlaylist(provider);

                if (!(playlist is null))
                {
                    playlists.AddOrUpdate(
                        provider.Priority,
                        playlist,
                        (key, oldValue) => playlist);
                }
            }

            return playlists
                .OrderBy(x => x.Key)
                .Select(x => x.Value);
        }

        public Playlist FetchProviderPlaylist(PlaylistProvider provider)
        {
            Playlist playlist = null;

            for (int i = 0; i < applicationSettings.DaysToCheck; i++)
            {
                DateTime date = DateTime.Now.AddDays(-i);

                playlist = LoadPlaylistFromCache(provider, date);
                
                if (playlist is null)
                {
                    playlist = DownloadPlaylist(provider, date);
                }
                
                if (!(playlist is null))
                {
                    break;
                }
            }

            if (!(playlist is null) &&
                !string.IsNullOrWhiteSpace(provider.ChannelNameOverride))
            {
                foreach (Channel channel in playlist.Channels)
                {
                    channel.Name = provider.ChannelNameOverride;
                }
            }
            
            return playlist;
        }

        Playlist LoadPlaylistFromCache(PlaylistProvider provider, DateTime date)
        {
            string content = cache.GetPlaylistFile(provider.Id, date);

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            return playlistFileBuilder.TryParseFile(content);
        }

        Playlist DownloadPlaylist(PlaylistProvider provider, DateTime date)
        {
            string url = string.Format(provider.UrlFormat, date);
            string fileContent = fileDownloader.TryDownloadString(url);

            Playlist playlist = playlistFileBuilder.TryParseFile(fileContent);

            if (Playlist.IsNullOrEmpty(playlist))
            {
                logger.Warn(MyOperation.PlaylistFetching, OperationStatus.Failure, new LogInfo(MyLogInfoKey.Url, url));
                return null;
            }

            logger.Info(MyOperation.PlaylistFetching, OperationStatus.Success, new LogInfo(MyLogInfoKey.Url, url));
            cache.StorePlaylistFile(provider.Id, date, fileContent);

            return playlist;
        }
    }
}
