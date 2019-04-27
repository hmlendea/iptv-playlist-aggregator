using System;
using System.Collections.Generic;
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

            return playlistFileBuilder.BuildFile(playlist);
        }

        void ProcessProvider(Playlist playlist, PlaylistProvider provider)
        {
            string m3uFile = FetchPlaylistFromProvider(provider);
            Playlist m3uPlaylist = playlistFileBuilder.ParseFile(m3uFile);
            
            foreach (Channel channel in m3uPlaylist.Channels)
            {
                ProcessChannel(playlist, channel);
            }
        }

        string FetchPlaylistFromProvider(PlaylistProvider provider)
        {
            using (WebClient client = new WebClient())
            {
                for (int i = 0; i < settings.DaysToCheck; i++)
                {
                    DateTime date = DateTime.Now.AddDays(-i);
                    string url = string.Format(provider.UrlFormat, date);

                    Console.WriteLine(url);

                    try
                    {
                        return client.DownloadString(url);
                    }
                    catch { }
                }
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
            finalChannel.Url = channel.Url;

            playlist.Channels.Add(finalChannel);
        }
    }
}
