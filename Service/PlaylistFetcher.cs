using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using NuciLog.Core;

using IptvPlaylistAggregator.Communication;
using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.Repositories;
using IptvPlaylistAggregator.Service.Mapping;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class PlaylistFetcher : IPlaylistFetcher
    {
        const string CacheFileNameFormat = "{0}_playlist_{1:yyyy-MM-dd}.m3u";
        
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly ApplicationSettings settings;
        readonly ILogger logger;

        public PlaylistFetcher(
            IPlaylistFileBuilder playlistFileBuilder,
            ApplicationSettings settings,
            ILogger logger)
        {
            this.playlistFileBuilder = playlistFileBuilder;
            this.settings = settings;
            this.logger = logger;
        }

        public IEnumerable<Playlist> FetchProviderPlaylists(IEnumerable<PlaylistProvider> providers)
        {
            ConcurrentDictionary<int, Playlist> playlists = new ConcurrentDictionary<int, Playlist>();

            logger.Info($"Getting the playlists from the providers ...");

            Parallel.ForEach(providers, provider =>
            {
                Playlist playlist = FetchProviderPlaylist(provider);

                if (!(playlist is null))
                {
                    playlists.AddOrUpdate(
                        provider.Priority,
                        playlist,
                        (key, oldValue) => playlist);
                }
            });

            return playlists
                .OrderBy(x => x.Key)
                .Select(x => x.Value);
        }

        public Playlist FetchProviderPlaylist(PlaylistProvider provider)
        {
            Playlist playlist = null;

            for (int i = 0; i < settings.DaysToCheck; i++)
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
            if (!Directory.Exists(settings.CacheDirectoryPath))
            {
                Directory.CreateDirectory(settings.CacheDirectoryPath);
            }

            string filePath = Path.Combine(
                settings.CacheDirectoryPath,
                string.Format(CacheFileNameFormat, providerId, date));

            File.WriteAllText(filePath, file);
        }

        Playlist LoadPlaylistFromCache(PlaylistProvider provider, DateTime date)
        {
            string filePath = Path.Combine(
                settings.CacheDirectoryPath,
                string.Format(CacheFileNameFormat, provider.Id, date));
            
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                
                return playlistFileBuilder.ParseFile(fileContent);
            }

            return null;
        }

        Playlist DownloadPlaylist(PlaylistProvider provider, DateTime date)
        {
            string url = string.Format(provider.UrlFormat, date);
            string fileContent = DownloadPlaylistFile(url);

            Playlist playlist = playlistFileBuilder.ParseFile(fileContent);

            if (Playlist.IsNullOrEmpty(playlist))
            {
                logger.Info(Operation.Unknown, OperationStatus.Failure, $"Getting playlist from '{url}'");
                return null;
            }

            logger.Info(Operation.Unknown, OperationStatus.Success, $"Getting playlist from '{url}'");
            StorePlaylistInCache(provider.Id, date, fileContent);

            return playlist;
        }

        string DownloadPlaylistFile(string url)
        {
            using (FileDownloader client = new FileDownloader(5000))
            {
                try
                {
                    return client.DownloadString(url);
                }
                catch { }
            }
            
            return null;
        }
    }
}
