using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.Repositories;
using IptvPlaylistAggregator.Service.Mapping;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class PlaylistAggregator : IPlaylistAggregator
    {
        readonly IPlaylistFetcher playlistFetcher;
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly IMediaSourceChecker mediaSourceChecker;
        readonly IChannelDefinitionRepository channelRepository;
        readonly IGroupRepository groupRepository;
        readonly IPlaylistProviderRepository playlistProviderRepository;
        readonly ApplicationSettings settings;

        readonly IDictionary<string, bool> pingedUrlsAliveStatus;

        IEnumerable<ChannelDefinition> channelDefinitions;
        IEnumerable<PlaylistProvider> playlistProviders;
        IDictionary<string, Group> groups;

        public PlaylistAggregator(
            IPlaylistFetcher playlistFetcher,
            IPlaylistFileBuilder playlistFileBuilder,
            IMediaSourceChecker mediaSourceChecker,
            IChannelDefinitionRepository channelRepository,
            IGroupRepository groupRepository,
            IPlaylistProviderRepository playlistProviderRepository,
            ApplicationSettings settings)
        {
            this.playlistFetcher = playlistFetcher;
            this.playlistFileBuilder = playlistFileBuilder;
            this.mediaSourceChecker = mediaSourceChecker;
            this.channelRepository = channelRepository;
            this.playlistProviderRepository = playlistProviderRepository;
            this.groupRepository = groupRepository;
            this.settings = settings;

            pingedUrlsAliveStatus = new Dictionary<string, bool>();
        }

        public string GatherPlaylist()
        {
            channelDefinitions = channelRepository
                .GetAll()
                .OrderBy(x => x.Name)
                .ToServiceModels();

            groups = groupRepository
                .GetAll()
                .Where(x => x.IsEnabled)
                .ToServiceModels()
                .ToDictionary(x => x.Id, x => x);

            playlistProviders = playlistProviderRepository
                .GetAll()
                .Where(x => x.IsEnabled)
                .ToServiceModels();

            Playlist playlist = new Playlist();
            IEnumerable<Channel> providerChannels = playlistFetcher
                .FetchProviderPlaylists(playlistProviders)
                .SelectMany(x => x.Channels)
                .GroupBy(x => x.Url)
                .Select(g => g.FirstOrDefault());

            foreach (Group group in groups.Values)
            {
                Console.WriteLine($"Getting channel URLs in group '{group.Id}' ...");

                IEnumerable<ChannelDefinition> channelDefsInGroup = channelDefinitions
                    .Where(x => x.IsEnabled && x.GroupId == group.Id);

                foreach (ChannelDefinition channelDef in channelDefsInGroup)
                {
                    string channelUrl = GetChannelUrl(channelDef, providerChannels);

                    if (!string.IsNullOrWhiteSpace(channelUrl))
                    {
                        Channel channel = new Channel();
                        channel.Id = channelDef.Id;
                        channel.Name = channelDef.Name;
                        channel.Group = groups[channelDef.GroupId].Name;
                        channel.LogoUrl = channelDef.LogoUrl;
                        channel.Number = playlist.Channels.Count + 1;
                        channel.Url = channelUrl;

                        playlist.Channels.Add(channel);
                    }
                }
            }

            if (settings.CanIncludeUnmatchedChannels)
            {
                Console.WriteLine($"Getting unmatched channels ...");

                IEnumerable<Channel> unmatchedChannels = GetUnmatchedChannels(providerChannels);

                foreach (Channel unmatchedChannel in unmatchedChannels)
                {
                    if (mediaSourceChecker.IsSourcePlayable(unmatchedChannel.Url))
                    {
                        Console.WriteLine($"Added unmatched channel: '{unmatchedChannel.Name}'");

                        unmatchedChannel.Number = playlist.Channels.Count + 1;
                        playlist.Channels.Add(unmatchedChannel);
                    }
                }
            }

            Console.WriteLine($"Finished aggregating {playlist.Channels.Count} channels");

            return playlistFileBuilder.BuildFile(playlist);
        }

        IEnumerable<Channel> GetUnmatchedChannels(IEnumerable<Channel> providerChannels)
        {
            IEnumerable<Channel> unmatchedChannels = providerChannels
                .GroupBy(x => x.Name)
                .Select(g => g.FirstOrDefault())
                .Where(x => channelDefinitions.All(y => !DoChannelNamesMatch(x.Name, y.Aliases)));

            IEnumerable<Channel> processedUnmatchedChannels = unmatchedChannels
                .Select(x => new Channel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = x.Name,
                    Group = groups["unknown"].Name,
                    Url = x.Url
                });

            return processedUnmatchedChannels.OrderBy(x => x.Name);
        }

        string GetChannelUrl(ChannelDefinition channelDef, IEnumerable<Channel> providerChannels)
        {
            foreach (Channel providerChannel in providerChannels)
            {
                if (!DoChannelNamesMatch(providerChannel.Name, channelDef.Aliases))
                {
                    continue;
                }

                bool isAlive = mediaSourceChecker.IsSourcePlayable(providerChannel.Url);

                if (isAlive)
                {
                    return providerChannel.Url;
                }
            }

            return null;
        }

        bool DoChannelNamesMatch(string name, IEnumerable<string> aliases)
        {
            return aliases.Any(alias =>
                NormaliseChannelName(name).Equals(NormaliseChannelName(alias)));
        }

        string NormaliseChannelName(string name)
        {
            string normalisedName = string.Empty;

            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                {
                    normalisedName += char.ToUpper(c);
                }
            }

            return normalisedName;
        }
    }
}
