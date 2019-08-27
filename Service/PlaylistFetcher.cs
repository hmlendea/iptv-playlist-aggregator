using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using NuciLog.Core;

using IptvPlaylistAggregator.Communication;
using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Logging;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class PlaylistFetcher : IPlaylistFetcher
    {
        const string CacheFileNameFormat = "{0}_playlist_{1:yyyy-MM-dd}.m3u";

        readonly IFileDownloader fileDownloader;        
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly ApplicationSettings applicationSettings;
        readonly CacheSettings cacheSettings;
        readonly ILogger logger;

        public PlaylistFetcher(
            IFileDownloader fileDownloader,
            IPlaylistFileBuilder playlistFileBuilder,
            ApplicationSettings applicationSettings,
            CacheSettings cacheSettings,
            ILogger logger)
        {
            this.fileDownloader = fileDownloader;
            this.playlistFileBuilder = playlistFileBuilder;
            this.applicationSettings = applicationSettings;
            this.cacheSettings = cacheSettings;
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

        void StorePlaylistInCache(string providerId, DateTime date, string file)
        {
            if (!Directory.Exists(cacheSettings.CacheDirectoryPath))
            {
                Directory.CreateDirectory(cacheSettings.CacheDirectoryPath);
            }

            string filePath = Path.Combine(
                cacheSettings.CacheDirectoryPath,
                string.Format(CacheFileNameFormat, providerId, date));

            File.WriteAllText(filePath, file);
        }

        Playlist LoadPlaylistFromCache(PlaylistProvider provider, DateTime date)
        {
            string filePath = Path.Combine(
                cacheSettings.CacheDirectoryPath,
                string.Format(CacheFileNameFormat, provider.Id, date));
            
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                
                return playlistFileBuilder.TryParseFile(fileContent);
            }

            return null;
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
            StorePlaylistInCache(provider.Id, date, fileContent);

            return playlist;
        }
    }
}
