using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using IptvPlaylistFetcher.Core.Configuration;
using IptvPlaylistFetcher.DataAccess.Repositories;
using IptvPlaylistFetcher.Service.Mapping;
using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service
{
    public sealed class PlaylistFetcher : IPlaylistFetcher
    {
        const string CacheFileNameFormat = "{0}_playlist_{1:yyyy-MM-dd}.m3u";

        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly IMediaStreamStatusChecker mediaStreamStatusChecker;
        readonly IChannelDefinitionRepository channelRepository;
        readonly IPlaylistProviderRepository playlistProviderRepository;
        readonly ApplicationSettings settings;

        readonly IDictionary<string, bool> pingedUrlsAliveStatus;

        IEnumerable<ChannelDefinition> channelDefinitions;
        IEnumerable<PlaylistProvider> playlistProviders;

        public PlaylistFetcher(
            IPlaylistFileBuilder playlistFileBuilder,
            IMediaStreamStatusChecker mediaStreamStatusChecker,
            IChannelDefinitionRepository channelRepository,
            IPlaylistProviderRepository playlistProviderRepository,
            ApplicationSettings settings)
        {
            this.playlistFileBuilder = playlistFileBuilder;
            this.mediaStreamStatusChecker = mediaStreamStatusChecker;
            this.channelRepository = channelRepository;
            this.playlistProviderRepository = playlistProviderRepository;
            this.settings = settings;

            pingedUrlsAliveStatus = new Dictionary<string, bool>();
        }

        public string GetPlaylistFile()
        {
            channelDefinitions = channelRepository.GetAll().ToServiceModels();
            playlistProviders = playlistProviderRepository.GetAll().ToServiceModels();

            Playlist playlist = new Playlist();
            IEnumerable<Playlist> providerPlaylists = FetchProviderPlaylists(playlistProviders);

            foreach (ChannelDefinition channelDef in channelDefinitions)
            {
                foreach (Playlist providerPlaylist in providerPlaylists)
                {
                    Channel providerChannel = providerPlaylist.Channels
                        .FirstOrDefault(x => channelDef.Aliases.Contains(x.Name));
                    
                    if (!(providerChannel is null) &&
                        mediaStreamStatusChecker.IsStreamAlive(providerChannel.Url))
                    {
                        Channel finalChannel = new Channel();
                        finalChannel.Id = channelDef.Id;
                        finalChannel.Name = channelDef.Name;
                        finalChannel.Category = channelDef.Category;
                        finalChannel.LogoUrl = channelDef.LogoUrl;
                        finalChannel.Url = providerChannel.Url;

                        playlist.Channels.Add(finalChannel);
                        break;
                    }
                }
            }

            IEnumerable<string> unmatchedChannelNames = providerPlaylists
                .SelectMany(x => x.Channels)
                .Where(x => channelDefinitions.All(y => !y.Aliases.Contains(x.Name)))
                .Select(x => x.Name)
                .Distinct()
                .OrderBy(x => x);
            
            foreach (string unmatchedChannelName in unmatchedChannelNames)
            {
                Console.WriteLine($"Unmatched channel: '{unmatchedChannelName}'");
            }

            playlist.Channels = playlist.Channels
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Name)
                .ToList();
            
            return playlistFileBuilder.BuildFile(playlist);
        }

        IEnumerable<Playlist> FetchProviderPlaylists(IEnumerable<PlaylistProvider> providers)
        {
            IList<Playlist> playlists = new List<Playlist>();

            foreach (PlaylistProvider provider in providers)
            {
                for (int i = 0; i < settings.DaysToCheck; i++)
                {
                    DateTime date = DateTime.Now.AddDays(-i);

                    Playlist playlist = LoadPlaylistFromCache(provider, date);
                    
                    if (playlist is null)
                    {
                        playlist = DownloadPlaylist(provider, date);
                    }
                    
                    if (!(playlist is null))
                    {
                        playlists.Add(playlist);
                        break;
                    }
                }
            }

            return playlists;
        }

        void SaveProviderPlaylistToCache(string providerId, string playlist)
        {
            if (!Directory.Exists(settings.CacheDirectoryPath))
            {
                Directory.CreateDirectory(settings.CacheDirectoryPath);
            }

            string filePath = Path.Combine(
                settings.CacheDirectoryPath,
                string.Format(CacheFileNameFormat, providerId, DateTime.Now));

            File.WriteAllText(filePath, playlist);
        }

        Playlist LoadPlaylistFromCache(PlaylistProvider provider, DateTime date)
        {
            string filePath = Path.Combine(
                settings.CacheDirectoryPath,
                string.Format(CacheFileNameFormat, provider.Id, DateTime.Now));
            
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

            using (WebClient client = new WebClient())
            {
                try
                {
                    string fileContent = client.DownloadString(url);
                    Playlist playlist = playlistFileBuilder.ParseFile(fileContent);

                    if (!playlist.IsEmpty)
                    {
                        SaveProviderPlaylistToCache(provider.Id, fileContent);
                        return playlist;
                    }
                }
                catch { }
            }

            return null;
        }
    }
}
