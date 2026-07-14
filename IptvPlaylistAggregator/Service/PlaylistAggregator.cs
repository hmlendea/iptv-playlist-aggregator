using System.Collections.Concurrent;

namespace IptvPlaylistAggregator.Service
{
    public sealed class PlaylistAggregator(
        IPlaylistFetcher playlistFetcher,
        IPlaylistFileBuilder playlistFileBuilder,
        IChannelMatcher channelMatcher,
        IMediaSourceChecker mediaSourceChecker,
        IFileRepository<ChannelDefinitionDataObject> channelRepository,
        IFileRepository<GroupDataObject> groupRepository,
        IFileRepository<PlaylistProviderDataObject> playlistProviderRepository,
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
                .ToDomainModels()
                .ToDictionary(group => group.Id, group => group);

            channelDefinitions = channelRepository
                .GetAll()
                .OrderBy(channelDefinition => groups[channelDefinition.GroupId].Priority)
                .ThenBy(channelDefinition => channelDefinition.Name)
                .ToDomainModels();

            playlistProviders = playlistProviderRepository
                .GetAll()
                .Where(provider => provider.IsEnabled)
                .ToDomainModels();

            IEnumerable<Channel> providerChannels = playlistFetcher
                .FetchProviderPlaylists(playlistProviders)
                .SelectMany(playlist => playlist.Channels);

            Playlist playlist = new();

            IEnumerable<Channel> channels = GetChannels(providerChannels);

            foreach (Channel channel in channels)
            {
                playlist.Channels.Add(channel);
            }

            return playlistFileBuilder.BuildFile(playlist);
        }

        private IEnumerable<Channel> GetChannels(IEnumerable<Channel> providerChannels)
        {
            IEnumerable<Channel> filteredProviderChannels = GetProviderChannels(providerChannels);

            logger.Info(MyOperation.ChannelMatching, OperationStatus.Started);

            IDictionary<string, Channel> enabledChannels = GetEnabledChannels(filteredProviderChannels)
                .ToDictionary(channel => channel.Id, channel => channel);
            IEnumerable<Channel> unmatchedChannels = GetUnmatchedChannels(filteredProviderChannels);

            List<Channel> channels = [];

            foreach (ChannelDefinition channelDefinition in channelDefinitions)
            {
                if (!enabledChannels.TryGetValue(channelDefinition.Id, out Channel channel))
                {
                    continue;
                }

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
                .Where(channelDefinition =>
                    channelDefinition.IsEnabled &&
                    groups[channelDefinition.GroupId].IsEnabled);

            Parallel.ForEach(enabledChannelDefinitions, channelDefinition =>
            {
                logger.Debug(
                    MyOperation.ChannelMatching,
                    OperationStatus.Started,
                    new LogInfo(MyLogInfoKey.Channel, channelDefinition.Name.Value));

                List<Channel> matchedChannels = [.. filteredProviderChannels
                    .Where(channel => channelMatcher.DoesMatch(
                        channelDefinition.Name,
                        channel.Name,
                        channel.Country))];

                if (!matchedChannels.Any())
                {
                    return;
                }

                logger.Debug(
                    MyOperation.ChannelMatching,
                    OperationStatus.InProgress,
                    new LogInfo(MyLogInfoKey.Channel, channelDefinition.Name.Value),
                    new LogInfo(MyLogInfoKey.ChannelsCount, matchedChannels.Count.ToString()));

                Channel matchedChannel = matchedChannels
                    .FirstOrDefault(channel => mediaSourceChecker.IsSourcePlayableAsync(channel.Url).Result);

                if (matchedChannel is null)
                {
                    logger.Debug(
                        MyOperation.ChannelMatching,
                        OperationStatus.Failure,
                        new LogInfo(MyLogInfoKey.Channel, channelDefinition.Name.Value));

                    return;
                }

                Channel resolvedChannel = new()
                {
                    Id = channelDefinition.Id,
                    Name = channelDefinition.Name.Value,
                    Country = channelDefinition.Country,
                    Group = groups[channelDefinition.GroupId].Name,
                    LogoUrl = channelDefinition.LogoUrl,
                    PlaylistId = matchedChannel.PlaylistId,
                    PlaylistChannelName = matchedChannel.PlaylistChannelName,
                    Url = matchedChannel.Url
                };

                channels.Add(resolvedChannel);

                logger.Debug(
                    MyOperation.ChannelMatching,
                    OperationStatus.Success,
                    new LogInfo(MyLogInfoKey.Channel, channelDefinition.Name.Value));
            });

            return channels;
        }

        private IEnumerable<Channel> GetUnmatchedChannels(IEnumerable<Channel> filteredProviderChannels)
        {
            ConcurrentBag<Channel> channels = [];

            if (!settings.AreUnmatchedChannelsIncluded)
            {
                return channels;
            }

            logger.Info(MyOperation.ChannelMatching, OperationStatus.InProgress, "Getting unmatched channels");

            IEnumerable<Channel> unmatchedChannels = filteredProviderChannels
                .Where(channel => channelDefinitions.All(
                    channelDefinition => !channelMatcher.DoesMatch(
                        channelDefinition.Name,
                        channel.Name,
                        channel.Country)))
                .GroupBy(channel => channel.Name)
                .Select(group => group.First())
                .OrderBy(channel => channel.Name);

            Parallel.ForEach(unmatchedChannels, unmatchedChannel =>
            {
                if (!mediaSourceChecker.IsSourcePlayableAsync(unmatchedChannel.Url).Result)
                {
                    return;
                }

                logger.Warn(
                    MyOperation.ChannelMatching,
                    OperationStatus.Failure,
                    new LogInfo(MyLogInfoKey.Channel, unmatchedChannel.Name));

                channels.Add(unmatchedChannel);
            });

            return channels;
        }

        private IEnumerable<Channel> GetProviderChannels(IEnumerable<Channel> channels)
        {
            logger.Info(
                MyOperation.ProviderChannelsFiltering,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.ChannelsCount, channels.Count().ToString()));

            IEnumerable<Channel> filteredChannels = channels
                .Where(channel => !string.IsNullOrWhiteSpace(channel.Url))
                .DistinctBy(channel => channel.Url);

            logger.Info(
                MyOperation.ProviderChannelsFiltering,
                OperationStatus.Success);

            return filteredChannels;
        }
    }
}
