using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            channelDefinitions = channelRepository
                .GetAll()
                .ToServiceModels();

            playlistProviders = playlistProviderRepository
                .GetAll()
                .Where(x => x.IsEnabled)
                .ToServiceModels();

            Playlist playlist = new Playlist();
            IEnumerable<Playlist> providerPlaylists = playlistFetcher.FetchProviderPlaylists(playlistProviders);

            foreach (ChannelDefinition channelDef in channelDefinitions.Where(x => x.IsEnabled))
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

                if (settings.CanIncludeUnmatchedChannels &&
                    mediaStreamStatusChecker.IsStreamAlive(unmatchedChannel.Url))
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
                .Where(x => channelDefinitions.All(y => !DoChannelNamesMatch(x.Name, y.Aliases)))
                .GroupBy(x => x.Name)
                .Select(g => g.FirstOrDefault());
            
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
                    providerPlaylist.Channels.Where(x => DoChannelNamesMatch(x.Name, channelDef.Aliases));
                
                foreach (Channel matchingChannel in matchingChannels)
                {
                    Console.Write(".");

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

        bool DoChannelNamesMatch(string name, IEnumerable<string> aliases)
        {
            return aliases.Any(alias =>
                NormaliseChannelName(name).Equals(NormaliseChannelName(alias)));
        }

        string NormaliseChannelName(string name)
        {
            return name
                .Where(c => char.IsLetterOrDigit(c))
                .Aggregate(
                    new StringBuilder(),
                    (current, next) => current.Append(next),
                    sb => sb.ToString())
                .ToUpper();
        }
    }
}
