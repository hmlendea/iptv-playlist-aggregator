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
        readonly IChannelDefinitionRepository channelRepository;
        readonly IPlaylistProviderRepository playlistProviderRepository;
        readonly ApplicationSettings settings;

        IEnumerable<ChannelDefinition> channelDefinitions;
        IEnumerable<PlaylistProvider> playlistProviders;

        public PlaylistFetcher(
            IPlaylistFileBuilder playlistFileBuilder,
            IChannelDefinitionRepository channelRepository,
            IPlaylistProviderRepository playlistProviderRepository,
            ApplicationSettings settings)
        {
            this.playlistFileBuilder = playlistFileBuilder;
            this.channelRepository = channelRepository;
            this.playlistProviderRepository = playlistProviderRepository;
            this.settings = settings;
        }

        public string GetPlaylistFile()
        {
            Playlist playlist = new Playlist();

            channelDefinitions = channelRepository.GetAll().ToServiceModels();
            playlistProviders = playlistProviderRepository.GetAll().ToServiceModels();
                
            foreach (PlaylistProvider provider in playlistProviders)
            {
                ProcessProvider(playlist, provider);
            }

            if (settings.AreCategoriesEnabled)
            {
                playlist.Channels = playlist.Channels
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Name)
                    .ToList();
            }
            else
            {
                playlist.Channels = playlist.Channels
                    .OrderBy(x => x.Name)
                    .ToList();
            }

            return playlistFileBuilder.BuildFile(playlist);
        }

        void ProcessProvider(Playlist playlist, PlaylistProvider provider)
        {
            Playlist m3uPlaylist = FetchPlaylistFromProvider(provider);

            if (Playlist.IsNullOrEmpty(m3uPlaylist))
            {
                return;
            }

            foreach (Channel channel in m3uPlaylist.Channels)
            {
                ProcessChannel(playlist, channel);
            }
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

        Playlist FetchPlaylistFromProvider(PlaylistProvider provider)
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
                    return playlist;
                }
            }

            return null;
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

            Console.WriteLine($"GET '{url}'");

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

        void ProcessChannel(Playlist playlist, Channel channel)
        {
            ChannelDefinition channelDef =
                channelDefinitions.FirstOrDefault(x => x.Aliases.Contains(channel.Name));
            
            if (channelDef is null)
            {
                Console.WriteLine($"Unknown channel '{channel.Name}'");
                return;
            }

            if (playlist.Channels.Any(x => x.Name.Equals(channelDef.Name)))
            {
                return;
            }
            
            Channel finalChannel = new Channel();
            finalChannel.Name = channelDef.Name;
            finalChannel.Category = channelDef.Category;
            finalChannel.Url = channel.Url;

            playlist.Channels.Add(finalChannel);
        }
    }
}
