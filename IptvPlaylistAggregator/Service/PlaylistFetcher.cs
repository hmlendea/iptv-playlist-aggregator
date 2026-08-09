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
        public IEnumerable<Playlist> FetchProviderPlaylists(IEnumerable<PlaylistProvider> providers)
        {
            ConcurrentDictionary<int, Playlist> playlists = new();
            List<PlaylistProvider> providerList = [.. providers];

            logger.Info(MyOperation.PlaylistFetching, OperationStatus.Started, "Fetching provider playlists");

            List<Task<Playlist>> tasks = new(providerList.Count);

            foreach (PlaylistProvider provider in providerList)
            {
                tasks.Add(FetchProviderPlaylistAsync(provider));
            }

            Task.WaitAll([.. tasks]);

            for (int i = 0; i < tasks.Count; i++)
            {
                PlaylistProvider provider = providerList[i];
                Playlist playlist = tasks[i].Result;

                if (Playlist.IsNullOrEmpty(playlist))
                {
                    continue;
                }

                playlists.AddOrUpdate(
                    provider.Priority,
                    playlist,
                    (key, oldValue) => playlist);

                string country = provider.Country;

                if (!string.IsNullOrWhiteSpace(country))
                {
                    foreach (Channel channel in playlist.Channels)
                    {
                        channel.Country = country;
                    }
                }
            }

            return playlists
                .OrderBy(entry => entry.Key)
                .Select(entry => entry.Value);
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

            string channelNameOverride = provider.ChannelNameOverride;
            bool hasChannelNameOverride = !string.IsNullOrWhiteSpace(channelNameOverride);

            foreach (Channel channel in playlist.Channels)
            {
                channel.PlaylistId = provider.Id;

                if (hasChannelNameOverride)
                {
                    channel.Name = channelNameOverride;
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
            DateTime currentDate = DateTime.UtcNow;
            string playlistFile = await DownloadPlaylistFileAsync(provider, currentDate);
            Playlist playlist = LoadPlaylistFromCache(provider, currentDate);

            playlist ??= playlistFileBuilder.TryParseFile(playlistFile);

            if (provider.IsCachingEnabled && !Playlist.IsNullOrEmpty(playlist))
            {
                cache.StorePlaylistFile(provider.Id, currentDate, playlistFile);
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
            DateTime currentDate = DateTime.UtcNow;

            for (int i = 1; i < applicationSettings.DaysToCheck; i++)
            {
                DateTime date = currentDate.AddDays(-i);

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
            if (!provider.IsCachingEnabled)
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
