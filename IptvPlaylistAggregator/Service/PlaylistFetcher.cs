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
    public sealed class PlaylistFetcher(
        IFileDownloader fileDownloader,
        IPlaylistFileBuilder playlistFileBuilder,
        ICacheManager cache,
        ApplicationSettings applicationSettings,
        ILogger logger) : IPlaylistFetcher
    {
        private readonly IFileDownloader fileDownloader = fileDownloader;
        private readonly IPlaylistFileBuilder playlistFileBuilder = playlistFileBuilder;
        private readonly ICacheManager cache = cache;
        private readonly ApplicationSettings applicationSettings = applicationSettings;
        private readonly ILogger logger = logger;

        public IEnumerable<Playlist> FetchProviderPlaylists(IEnumerable<PlaylistProvider> providers)
        {
            ConcurrentDictionary<int, Playlist> playlists = new();

            logger.Info(MyOperation.PlaylistFetching, OperationStatus.Started, "Fetching provider playlists");

            List<Task> tasks = [];

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

                        if (!string.IsNullOrWhiteSpace(provider.Country))
                        {
                            foreach (Channel channel in playlist.Channels)
                            {
                                channel.Country = provider.Country;
                            }
                        }
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

            foreach (Channel channel in playlist.Channels)
            {
                channel.PlaylistId = provider.Id;

                if (!string.IsNullOrWhiteSpace(provider.ChannelNameOverride))
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

        private async Task<Playlist> GetPlaylistAsync(PlaylistProvider provider)
        {
            Playlist playlist = await GetPlaylistForTodayAsync(provider);

            if (Playlist.IsNullOrEmpty(playlist))
            {
                playlist = GetPlaylistForPastDays(provider);
            }

            return playlist;
        }

        private async Task<Playlist> GetPlaylistForTodayAsync(PlaylistProvider provider)
        {
            string playlistFile = await DownloadPlaylistFileAsync(provider, DateTime.UtcNow);
            Playlist playlist = LoadPlaylistFromCache(provider, DateTime.UtcNow);

            playlist ??= playlistFileBuilder.TryParseFile(playlistFile);

            if (provider.AllowCaching && !Playlist.IsNullOrEmpty(playlist))
            {
                cache.StorePlaylistFile(provider.Id, DateTime.UtcNow, playlistFile);
            }

            return playlist;
        }

        private Playlist GetPlaylistForPastDays(PlaylistProvider provider)
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

                if (playlist is not null)
                {
                    break;
                }
            }

            return playlist;
        }

        private Playlist LoadPlaylistFromCache(PlaylistProvider provider, DateTime date)
        {
            if (!provider.AllowCaching)
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

        private async Task<string> DownloadPlaylistFileAsync(PlaylistProvider provider, DateTime date)
            => await fileDownloader.TryDownloadStringAsync(string.Format(provider.UrlFormat, date));
    }
}
