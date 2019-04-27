using System;
using System.Collections.Generic;
using System.Linq;

using IptvPlaylistFetcher.Core.Configuration;
using IptvPlaylistFetcher.DataAccess.Repositories;
using IptvPlaylistFetcher.Service.Mapping;
using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service
{
    public sealed class PlaylistAggregator : IPlaylistAggregator
    {
        readonly IPlaylistFetcher playlistFetcher;
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly IMediaStreamStatusChecker mediaStreamStatusChecker;
        readonly IChannelDefinitionRepository channelRepository;
        readonly IPlaylistProviderRepository playlistProviderRepository;
        readonly ApplicationSettings settings;

        readonly IDictionary<string, bool> pingedUrlsAliveStatus;

        IEnumerable<ChannelDefinition> channelDefinitions;
        IEnumerable<PlaylistProvider> playlistProviders;

        public PlaylistAggregator(
            IPlaylistFetcher playlistFetcher,
            IPlaylistFileBuilder playlistFileBuilder,
            IMediaStreamStatusChecker mediaStreamStatusChecker,
            IChannelDefinitionRepository channelRepository,
            IPlaylistProviderRepository playlistProviderRepository,
            ApplicationSettings settings)
        {
            this.playlistFetcher = playlistFetcher;
            this.playlistFileBuilder = playlistFileBuilder;
            this.mediaStreamStatusChecker = mediaStreamStatusChecker;
            this.channelRepository = channelRepository;
            this.playlistProviderRepository = playlistProviderRepository;
            this.settings = settings;

            pingedUrlsAliveStatus = new Dictionary<string, bool>();
        }

        public string GatherPlaylist()
        {
            channelDefinitions = channelRepository.GetAll().ToServiceModels();
            playlistProviders = playlistProviderRepository.GetAll().ToServiceModels();

            Playlist playlist = new Playlist();
            IEnumerable<Playlist> providerPlaylists = playlistFetcher.FetchProviderPlaylists(playlistProviders);

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
    }
}
