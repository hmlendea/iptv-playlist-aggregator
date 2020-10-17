using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            List<Task> tasks = new List<Task>();

            foreach (PlaylistProvider provider in providers)
            {
                Task task = Task.Run(async () =>
                {
                    Playlist playlist = await FetchProviderPlaylistAsync(provider);

                    if (!Playlist.IsNullOrEmpty(playlist))
                    {
                        playlists.AddOrUpdate(
                            provider.Priority,
                            playlist,
                            (key, oldValue) => playlist);
                    }
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            return playlists
                .OrderBy(x => x.Key)
                .Select(x => x.Value);
        }

        public async Task<Playlist> FetchProviderPlaylistAsync(PlaylistProvider provider)
        {
            Playlist playlist = await GetPlaylistAsync(provider);

            if (Playlist.IsNullOrEmpty(playlist))
            {
                logger.Debug(
                    MyOperation.PlaylistFetching,
                    OperationStatus.Failure,
                    new LogInfo(MyLogInfoKey.Provider, provider.Name));

                return null;
            }

            if (!string.IsNullOrWhiteSpace(provider.ChannelNameOverride))
            {
                foreach (Channel channel in playlist.Channels)
                {
                    channel.Name = provider.ChannelNameOverride;
                }
            }

            logger.Debug(
                MyOperation.PlaylistFetching,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.Provider, provider.Name));
                
            return playlist;
        }

        async Task<Playlist> GetPlaylistAsync(PlaylistProvider provider)
        {
            Playlist playlist = await GetPlaylistForTodayAsync(provider);
            
            if (Playlist.IsNullOrEmpty(playlist))
            {
                playlist = GetPlaylistForPastDays(provider);
            }

            return playlist;
        }

        async Task<Playlist> GetPlaylistForTodayAsync(PlaylistProvider provider)
        {
            string playlistFile = await DownloadPlaylistFileAsync(provider, DateTime.UtcNow);
            Playlist playlist = LoadPlaylistFromCache(provider, DateTime.UtcNow);

            if (playlist is null)
            {
                playlist = playlistFileBuilder.TryParseFile(playlistFile);
            }

            if (provider.AllowCaching && !Playlist.IsNullOrEmpty(playlist))
            {
                cache.StorePlaylistFile(provider.Id, DateTime.UtcNow, playlistFile);
            }

            return playlist;
        }

        Playlist GetPlaylistForPastDays(PlaylistProvider provider)
        {
            if (!provider.UrlFormat.Contains("{0"))
            {
                return null;
            }

            Playlist playlist = null;

            for (int i = 1; i < applicationSettings.DaysToCheck; i++)
            {
                DateTime date = DateTime.UtcNow.AddDays(-i);

                playlist = LoadPlaylistFromCache(provider, date);

                if (!(playlist is null))
                {
                    break;
                }
            }

            return playlist;
        }

        Playlist LoadPlaylistFromCache(PlaylistProvider provider, DateTime date)
        {
            if (provider.AllowCaching)
            {
                return null;
            }

            string content = cache.GetPlaylistFile(provider.Id, date);

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            return playlistFileBuilder.TryParseFile(content);
        }

        async Task<string> DownloadPlaylistFileAsync(PlaylistProvider provider, DateTime date)
        {
            string url = string.Format(provider.UrlFormat, date);
            return await fileDownloader.TryDownloadStringAsync(url);
        }
    }
}
