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
            Playlist playlist = null;

            for (int i = 0; i < applicationSettings.DaysToCheck; i++)
            {
                DateTime date = DateTime.Now.AddDays(-i);

                playlist = LoadPlaylistFromCache(provider, date);
                
                if (playlist is null)
                {
                    string playlistFile = await DownloadPlaylistFileAsync(provider, date);
                    playlist = playlistFileBuilder.TryParseFile(playlistFile);

                    if (!Playlist.IsNullOrEmpty(playlist))
                    {
                        cache.StorePlaylistFile(provider.Id, date, playlistFile);
                    }
                }
                
                if (!(playlist is null))
                {
                    break;
                }
            }

            if (Playlist.IsNullOrEmpty(playlist))
            {
                logger.Warn(
                    MyOperation.PlaylistFetching,
                    OperationStatus.Failure,
                    new LogInfo(MyLogInfoKey.Provider, provider.Id));

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
                new LogInfo(MyLogInfoKey.Provider, provider.Id));
                
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

        async Task<string> DownloadPlaylistFileAsync(PlaylistProvider provider, DateTime date)
        {
            string url = string.Format(provider.UrlFormat, date);
            return await fileDownloader.TryDownloadStringAsync(url);
        }
    }
}
