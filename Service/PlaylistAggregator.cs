using System.Collections.Generic;
using System.Collections.Concurrent;
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
    public sealed class PlaylistAggregator : IPlaylistAggregator
    {
        readonly IPlaylistFetcher playlistFetcher;
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly IChannelMatcher channelMatcher;
        readonly IMediaSourceChecker mediaSourceChecker;
        readonly IDnsResolver dnsResolver;
        readonly IRepository<ChannelDefinitionEntity> channelRepository;
        readonly IRepository<GroupEntity> groupRepository;
        readonly IRepository<PlaylistProviderEntity> playlistProviderRepository;
        readonly ApplicationSettings settings;
        readonly ILogger logger;

        IEnumerable<ChannelDefinition> channelDefinitions;
        IEnumerable<PlaylistProvider> playlistProviders;
        IDictionary<string, Group> groups;

        public PlaylistAggregator(
            IPlaylistFetcher playlistFetcher,
            IPlaylistFileBuilder playlistFileBuilder,
            IChannelMatcher channelMatcher,
            IMediaSourceChecker mediaSourceChecker,
            IDnsResolver dnsResolver,
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
            this.dnsResolver = dnsResolver;
            this.channelRepository = channelRepository;
            this.playlistProviderRepository = playlistProviderRepository;
            this.groupRepository = groupRepository;
            this.settings = settings;
            this.logger = logger;
        }

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
            
            Playlist playlist = new Playlist();
            IEnumerable<Channel> filteredProviderChannels = GetProvicerChannels(providerChannels, channelDefinitions);
            IEnumerable<ChannelDefinition> enabledChannelDefinitions = channelDefinitions
                .Where(x => x.IsEnabled && groups[x.GroupId].IsEnabled);

            logger.Info(MyOperation.ChannelMatching, OperationStatus.Started);

            foreach (ChannelDefinition channelDef in enabledChannelDefinitions)
            {
                logger.Debug(
                    MyOperation.ChannelMatching,
                    OperationStatus.Started,
                    new LogInfo(MyLogInfoKey.Channel, channelDef.Name.Value));

                List<Channel> matchedChannels = filteredProviderChannels
                    .Where(x => channelMatcher.DoesMatch(channelDef.Name, x.Name))
                    .ToList();
                
                if (!matchedChannels.Any())
                {
                    continue;
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

                logger.Debug(
                    MyOperation.ChannelMatching,
                    OperationStatus.Success,
                    new LogInfo(MyLogInfoKey.Channel, channelDef.Name.Value));
            }

            if (settings.CanIncludeUnmatchedChannels)
            {
                logger.Info(MyOperation.ChannelMatching, OperationStatus.InProgress, $"Getting unmatched channels");

                IEnumerable<Channel> unmatchedChannels = filteredProviderChannels
                    .Where(x => channelDefinitions.All(y => !channelMatcher.DoesMatch(y.Name, x.Name)))
                    .GroupBy(x => x.Name)
                    .Select(g => g.First())
                    .OrderBy(x => x.Name);

                foreach (Channel unmatchedChannel in unmatchedChannels.Where(x => mediaSourceChecker.IsSourcePlayableAsync(x.Url).Result))
                {
                    logger.Warn(MyOperation.ChannelMatching, OperationStatus.Failure, new LogInfo(MyLogInfoKey.Channel, unmatchedChannel.Name));

                    unmatchedChannel.Number = playlist.Channels.Count + 1;
                    playlist.Channels.Add(unmatchedChannel);
                }
            }

            logger.Debug(
                MyOperation.ChannelMatching,
                OperationStatus.Success,
                new LogInfo(MyLogInfoKey.ChannelsCount, playlist.Channels.Count.ToString()));

            return playlistFileBuilder.BuildFile(playlist);
        }

        IEnumerable<Channel> GetProvicerChannels(
            IList<Channel> channels,
            IEnumerable<ChannelDefinition> channelDefinitions)
        {
            logger.Info(
                MyOperation.ProviderChannelsFiltering,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.ChannelsCount, channels.Count()));

            List<Task> tasks = new List<Task>();
            IEnumerable<Channel> filteredChannels = channels
                .Where(x => !string.IsNullOrWhiteSpace(dnsResolver.ResolveUrl(x.Url)))
                .GroupBy(x => dnsResolver.ResolveUrl(x.Url))
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
