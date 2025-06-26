using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NuciDAL.Repositories;
using NuciLog.Core;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Logging;
using IptvPlaylistAggregator.Service.Mapping;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class PlaylistAggregator(
        IPlaylistFetcher playlistFetcher,
        IPlaylistFileBuilder playlistFileBuilder,
        IChannelMatcher channelMatcher,
        IMediaSourceChecker mediaSourceChecker,
        IFileRepository<ChannelDefinitionEntity> channelRepository,
        IFileRepository<GroupEntity> groupRepository,
        IFileRepository<PlaylistProviderEntity> playlistProviderRepository,
        ApplicationSettings settings,
        ILogger logger) : IPlaylistAggregator
    {
        private IEnumerable<ChannelDefinition> channelDefinitions;
        private IEnumerable<PlaylistProvider> playlistProviders;
        private IDictionary<string, Group> groups;

        public string GatherPlaylist()
        {
            groups = groupRepository
                .GetAll()
                .ToServiceModels()
                .ToDictionary(x => x.Id, x => x);

            channelDefinitions = channelRepository
                .GetAll()
                .OrderBy(x => groups[x.GroupId].Priority)
                .ThenBy(x => x.Name)
                .ToServiceModels();

            playlistProviders = playlistProviderRepository
                .GetAll()
                .Where(x => x.IsEnabled)
                .ToServiceModels();

            IList<Channel> providerChannels = playlistFetcher
                .FetchProviderPlaylists(playlistProviders)
                .SelectMany(x => x.Channels)
                .ToList();

            Playlist playlist = new();

            IEnumerable<Channel> channels = GetChannels(providerChannels);

            foreach (Channel channel in channels)
            {
                playlist.Channels.Add(channel);
            }

            return playlistFileBuilder.BuildFile(playlist);
        }

        private IEnumerable<Channel> GetChannels(IList<Channel> providerChannels)
        {
            IEnumerable<Channel> filteredProviderChannels = GetProvicerChannels(providerChannels);

            logger.Info(MyOperation.ChannelMatching, OperationStatus.Started);

            IDictionary<string, Channel> enabledChannels = GetEnabledChannels(filteredProviderChannels).ToDictionary(x => x.Id, x => x);
            IEnumerable<Channel> unmatchedChannels = GetUnmatchedChannels(filteredProviderChannels);

            List<Channel> channels = [];

            foreach (ChannelDefinition channelDef in channelDefinitions)
            {
                if (!enabledChannels.ContainsKey(channelDef.Id))
                {
                    continue;
                }

                Channel channel = enabledChannels[channelDef.Id];
                channel.Number = channels.Count + 1;

                channels.Add(channel);
            }

            foreach (Channel channel in unmatchedChannels)
            {
                channel.Number = channels.Count + 1;
                channels.Add(channel);
            }

            logger.Debug(
                MyOperation.ChannelMatching,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.ChannelsCount, channels.Count.ToString()));

            return channels;
        }

        private IEnumerable<Channel> GetEnabledChannels(IEnumerable<Channel> filteredProviderChannels)
        {
            ConcurrentBag<Channel> channels = [];
            IEnumerable<ChannelDefinition> enabledChannelDefinitions = channelDefinitions
                .Where(x => x.IsEnabled && groups[x.GroupId].IsEnabled);

            Parallel.ForEach(enabledChannelDefinitions, channelDef =>
            {
                logger.Debug(
                    MyOperation.ChannelMatching,
                    OperationStatus.Started,
                    new LogInfo(MyLogInfoKey.Channel, channelDef.Name.Value));

                List<Channel> matchedChannels = filteredProviderChannels
                    .Where(x => channelMatcher.DoesMatch(channelDef.Name, x.Name, x.Country))
                    .ToList();

                if (!matchedChannels.Any())
                {
                    return;
                }

                logger.Debug(
                    MyOperation.ChannelMatching,
                    OperationStatus.InProgress,
                    new LogInfo(MyLogInfoKey.Channel, channelDef.Name.Value),
                    new LogInfo(MyLogInfoKey.ChannelsCount, matchedChannels.Count));

                Channel matchedChannel = matchedChannels.FirstOrDefault(x => mediaSourceChecker.IsSourcePlayableAsync(x.Url).Result);

                if (matchedChannel is null)
                {
                    logger.Debug(
                        MyOperation.ChannelMatching,
                        OperationStatus.Failure,
                        new LogInfo(MyLogInfoKey.Channel, channelDef.Name.Value));

                    return;
                }

                Channel channel = new()
                {
                    Id = channelDef.Id,
                    Name = channelDef.Name.Value,
                    Country = channelDef.Country,
                    Group = groups[channelDef.GroupId].Name,
                    LogoUrl = channelDef.LogoUrl,
                    PlaylistId = matchedChannel.PlaylistId,
                    PlaylistChannelName = matchedChannel.PlaylistChannelName,
                    Url = matchedChannel.Url
                };

                channels.Add(channel);

                logger.Debug(
                    MyOperation.ChannelMatching,
                    OperationStatus.Success,
                    new LogInfo(MyLogInfoKey.Channel, channelDef.Name.Value));
            });

            return channels;
        }

        private IEnumerable<Channel> GetUnmatchedChannels(IEnumerable<Channel> filteredProviderChannels)
        {
            ConcurrentBag<Channel> channels = [];

            if (!settings.CanIncludeUnmatchedChannels)
            {
                return channels;
            }

            logger.Info(MyOperation.ChannelMatching, OperationStatus.InProgress, $"Getting unmatched channels");

            IEnumerable<Channel> unmatchedChannels = filteredProviderChannels
                .Where(x => channelDefinitions.All(y => !channelMatcher.DoesMatch(y.Name, x.Name, x.Country)))
                .GroupBy(x => x.Name)
                .Select(g => g.First())
                .OrderBy(x => x.Name);

            Parallel.ForEach(unmatchedChannels, unmatchedChannel =>
            {
                if (!mediaSourceChecker.IsSourcePlayableAsync(unmatchedChannel.Url).Result)
                {
                    return;
                }

                logger.Warn(MyOperation.ChannelMatching, OperationStatus.Failure, new LogInfo(MyLogInfoKey.Channel, unmatchedChannel.Name));

                channels.Add(unmatchedChannel);
            });

            return channels;
        }

        private IEnumerable<Channel> GetProvicerChannels(
            IList<Channel> channels)
        {
            logger.Info(
                MyOperation.ProviderChannelsFiltering,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.ChannelsCount, channels.Count));

            List<Task> tasks = [];
            IEnumerable<Channel> filteredChannels = channels
                .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                .GroupBy(x => x.Url)
                .Select(g => g.First())
                .OrderBy(x => channels.IndexOf(x))
                .ToList();

            logger.Info(
                MyOperation.ProviderChannelsFiltering,
                OperationStatus.Success);

            return filteredChannels;
        }
    }
}
