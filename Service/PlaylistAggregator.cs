using System;
using System.Collections.Generic;
using System.Linq;

using NuciDAL.Repositories;
using NuciLog.Core;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Logging;
using IptvPlaylistAggregator.Service.Mapping;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class PlaylistAggregator : IPlaylistAggregator
    {
        readonly IPlaylistFetcher playlistFetcher;
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly IChannelMatcher channelMatcher;
        readonly IMediaSourceChecker mediaSourceChecker;
        readonly IRepository<ChannelDefinitionEntity> channelRepository;
        readonly IRepository<GroupEntity> groupRepository;
        readonly IRepository<PlaylistProviderEntity> playlistProviderRepository;
        readonly ApplicationSettings settings;
        readonly ILogger logger;

        readonly IDictionary<string, bool> pingedUrlsAliveStatus;

        IEnumerable<ChannelDefinition> channelDefinitions;
        IEnumerable<PlaylistProvider> playlistProviders;
        IDictionary<string, Group> groups;

        public PlaylistAggregator(
            IPlaylistFetcher playlistFetcher,
            IPlaylistFileBuilder playlistFileBuilder,
            IChannelMatcher channelMatcher,
            IMediaSourceChecker mediaSourceChecker,
            IRepository<ChannelDefinitionEntity> channelRepository,
            IRepository<GroupEntity> groupRepository,
            IRepository<PlaylistProviderEntity> playlistProviderRepository,
            ApplicationSettings settings,
            ILogger logger)
        {
            this.playlistFetcher = playlistFetcher;
            this.playlistFileBuilder = playlistFileBuilder;
            this.channelMatcher = channelMatcher;
            this.mediaSourceChecker = mediaSourceChecker;
            this.channelRepository = channelRepository;
            this.playlistProviderRepository = playlistProviderRepository;
            this.groupRepository = groupRepository;
            this.settings = settings;
            this.logger = logger;

            pingedUrlsAliveStatus = new Dictionary<string, bool>();
        }

        public string GatherPlaylist()
        {
            groups = groupRepository
                .GetAll()
                .Where(x => x.IsEnabled)
                .ToServiceModels()
                .ToDictionary(x => x.Id, x => x);

            channelDefinitions = channelRepository
                .GetAll()
                .Where (x => x.IsEnabled && groups[x.GroupId].IsEnabled)
                .OrderBy(x => groups[x.GroupId].Priority)
                .ThenBy(x => x.Name)
                .ToServiceModels();

            playlistProviders = playlistProviderRepository
                .GetAll()
                .Where(x => x.IsEnabled)
                .OrderBy(x => x.Priority)
                .ToServiceModels();

            Playlist playlist = new Playlist();

            IEnumerable<Channel> providerChannels = playlistFetcher
                .FetchProviderPlaylists(playlistProviders)
                .SelectMany(x => x.Channels);
            
            IEnumerable<Channel> filteredProviderChannels = FilterProviderChannels(providerChannels, channelDefinitions);

            foreach (ChannelDefinition channelDef in channelDefinitions)
            {
                Channel matchedChannel = filteredProviderChannels
                    .FirstOrDefault(x => channelMatcher.DoesMatch(channelDef.Name, x.Name));

                if (matchedChannel is null)
                {
                    continue;
                }

                Channel channel = new Channel();
                channel.Id = channelDef.Id;
                channel.Name = channelDef.Name.Value;
                channel.Group = groups[channelDef.GroupId].Name;
                channel.LogoUrl = channelDef.LogoUrl;
                channel.Number = playlist.Channels.Count + 1;
                channel.Url = matchedChannel.Url;

                playlist.Channels.Add(channel);
            }

            if (settings.CanIncludeUnmatchedChannels)
            {
                logger.Info(MyOperation.ChannelMatching, OperationStatus.InProgress, $"Getting unmatched channels");

                IEnumerable<Channel> unmatchedChannels = GetUnmatchedChannels(filteredProviderChannels);

                foreach (Channel unmatchedChannel in unmatchedChannels)
                {
                    if (!mediaSourceChecker.IsSourcePlayable(unmatchedChannel.Url))
                    {
                        continue;
                    }

                    logger.Warn(MyOperation.ChannelMatching, OperationStatus.Failure, new LogInfo(MyLogInfoKey.Channel, unmatchedChannel.Name));

                    unmatchedChannel.Number = playlist.Channels.Count + 1;
                    playlist.Channels.Add(unmatchedChannel);
                }
            }

            logger.Info(MyOperation.ChannelMatching, OperationStatus.Success, new LogInfo(MyLogInfoKey.ChannelsCount, playlist.Channels.Count.ToString()));

            return playlistFileBuilder.BuildFile(playlist);
        }

        IEnumerable<Channel> FilterProviderChannels(
            IEnumerable<Channel> channels,
            IEnumerable<ChannelDefinition> channelDefinitions)
        {
            logger.Info(
                MyOperation.ProviderChannelsFiltering,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.ChannelsCount, channels.Count()));

            List<Channel> filteredChannels = new List<Channel>();

            IEnumerable<Channel> uniqueChannels = channels
                .GroupBy(x => x.Url)
                .Select(g => g.FirstOrDefault())
                .OrderBy(x => channelMatcher.NormaliseName(x.Name));

            Dictionary<string, string> normalisedNames = new Dictionary<string, string>();

            var a = uniqueChannels
                    .GroupBy(channel => channelMatcher.NormaliseName(channel.Name))
                    .Select(g => g.FirstOrDefault())
                    .OrderBy(x => channelMatcher.NormaliseName(x.Name));

            foreach (Channel channel in a)
            {
                string normalisedName = channelMatcher.NormaliseName(channel.Name);

                foreach (ChannelDefinition channelDefinition in channelDefinitions)
                {
                    if (channelMatcher.DoesMatch(channelDefinition.Name, channel.Name))
                    {
                        normalisedName = channelMatcher.NormaliseName(channelDefinition.Name.Value);
                    }
                }

                normalisedNames.Add(channel.Name, normalisedName);
            }

            foreach (Channel channel in uniqueChannels)
            {
                string normalisedName = normalisedNames[channel.Name];

                if (filteredChannels.Any(x => normalisedNames[x.Name] == channel.Name) ||
                    !mediaSourceChecker.IsSourcePlayable(channel.Url))
                {
                    continue;
                }

                filteredChannels.Add(channel);
            }

            logger.Info(
                MyOperation.ProviderChannelsFiltering,
                OperationStatus.Started);

            return filteredChannels;
        }

        IEnumerable<Channel> GetUnmatchedChannels(IEnumerable<Channel> providerChannels)
        {
            IEnumerable<Channel> unmatchedChannels = providerChannels
                .GroupBy(x => x.Name)
                .Select(g => g.FirstOrDefault())
                .Where(x => channelDefinitions.All(y => !channelMatcher.DoesMatch(y.Name, x.Name)));

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
    }
}
