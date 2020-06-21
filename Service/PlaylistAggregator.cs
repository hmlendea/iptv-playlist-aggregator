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

            IEnumerable<Channel> channels = GetChannels(providerChannels);

            foreach (Channel channel in channels)
            {
                playlist.Channels.Add(channel);
            }

            return playlistFileBuilder.BuildFile(playlist);
        }

        IEnumerable<Channel> GetChannels(IList<Channel> providerChannels)
        {
            IEnumerable<Channel> filteredProviderChannels = GetProvicerChannels(providerChannels, channelDefinitions);

            logger.Info(MyOperation.ChannelMatching, OperationStatus.Started);

            IDictionary<string, Channel> enabledChannels = GetEnabledChannels(filteredProviderChannels).ToDictionary(x => x.Id, x => x);
            IEnumerable<Channel> unmatchedChannels = GetUnmatchedChannels(filteredProviderChannels);
            
            List<Channel> channels = new List<Channel>();

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

        IEnumerable<Channel> GetEnabledChannels(IEnumerable<Channel> filteredProviderChannels)
        {
            ConcurrentBag<Channel> channels = new ConcurrentBag<Channel>();
            IEnumerable<ChannelDefinition> enabledChannelDefinitions = channelDefinitions
                .Where(x => x.IsEnabled && groups[x.GroupId].IsEnabled);

            Parallel.ForEach(enabledChannelDefinitions, channelDef =>
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

                Channel channel = new Channel();
                channel.Id = channelDef.Id;
                channel.Name = channelDef.Name.Value;
                channel.Group = groups[channelDef.GroupId].Name;
                channel.LogoUrl = channelDef.LogoUrl;
                channel.Url = matchedChannel.Url;

                channels.Add(channel);

                logger.Debug(
                    MyOperation.ChannelMatching,
                    OperationStatus.Success,
                    new LogInfo(MyLogInfoKey.Channel, channelDef.Name.Value));
            });
            
            return channels;
        }

        IEnumerable<Channel> GetUnmatchedChannels(IEnumerable<Channel> filteredProviderChannels)
        {
            ConcurrentBag<Channel> channels = new ConcurrentBag<Channel>();

            if (!settings.CanIncludeUnmatchedChannels)
            {
                return channels;
            }

            logger.Info(MyOperation.ChannelMatching, OperationStatus.InProgress, $"Getting unmatched channels");

            IEnumerable<Channel> unmatchedChannels = filteredProviderChannels
                .Where(x => channelDefinitions.All(y => !channelMatcher.DoesMatch(y.Name, x.Name)))
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
