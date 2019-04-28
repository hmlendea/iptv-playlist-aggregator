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
                string channelUrl = GetChannelUrl(channelDef, providerPlaylists);

                if (!string.IsNullOrWhiteSpace(channelUrl))
                {
                    Channel channel = new Channel();
                    channel.Id = channelDef.Id;
                    channel.Name = channelDef.Name;
                    channel.Group = channelDef.Group;
                    channel.LogoUrl = channelDef.LogoUrl;
                    channel.Url = channelUrl;

                    playlist.Channels.Add(channel);
                }
            }

            IEnumerable<Channel> unmatchedChannelNames = GetUnmatchedChannels(providerPlaylists);
            
            foreach (Channel unmatchedChannel in unmatchedChannelNames)
            {
                Console.WriteLine($"Unmatched channel: '{unmatchedChannel.Name}'");

                if (settings.CanIncludeUnmatchedChannels)
                {
                    playlist.Channels.Add(unmatchedChannel);
                }
            }

            playlist.Channels = playlist.Channels
                .OrderBy(x => x.Group)
                .ThenBy(x => x.Name)
                .ToList();
            
            return playlistFileBuilder.BuildFile(playlist);
        }

        IEnumerable<Channel> GetUnmatchedChannels(IEnumerable<Playlist> providerPlaylists)
        {
            IEnumerable<Channel> unmatchedChannels = providerPlaylists
                .SelectMany(x => x.Channels)
                .Where(x => channelDefinitions.All(y => !y.Aliases.Contains(x.Name)));
            
            IEnumerable<Channel> processedUnmatchedChannels = unmatchedChannels
                .Select(x => new Channel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = x.Name,
                    Group = "zzz UNKNOWN",
                    Url = x.Url
                });
            
            return processedUnmatchedChannels.OrderBy(x => x.Name);
        }

        string GetChannelUrl(ChannelDefinition channelDef, IEnumerable<Playlist> providerPlaylists)
        {
            Console.WriteLine($"Getting URL for '{channelDef.Name}'");

            foreach (Playlist providerPlaylist in providerPlaylists)
            {
                IEnumerable<Channel> matchingChannels =
                    providerPlaylist.Channels.Where(x => channelDef.Aliases.Contains(x.Name));
                
                foreach (Channel matchingChannel in matchingChannels)
                {
                    Console.Write(".");
                    // TODO: FIX THIS
                    // Short-circuit until I handle this properly
                    if (matchingChannel.Url.EndsWith(".m3u") ||
                        matchingChannel.Url.EndsWith(".m3u8"))
                    {
                        //continue;
                    }

                    bool isAlive = mediaStreamStatusChecker.IsStreamAlive(matchingChannel.Url);

                    if (isAlive)
                    {
                        Console.WriteLine();
                        return matchingChannel.Url;
                    }
                }
            }

            Console.WriteLine();
            return null;
        }
    }
}
