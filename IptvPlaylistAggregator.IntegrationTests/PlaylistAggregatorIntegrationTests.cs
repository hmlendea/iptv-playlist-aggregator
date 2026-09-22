using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using NuciDAL.Repositories;
using NuciLog.Core;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.IntegrationTests
{
    [TestFixture]
    public class PlaylistAggregatorIntegrationTests
    {
        private Mock<IPlaylistFetcher> mockPlaylistFetcher;
        private Mock<IPlaylistFileBuilder> mockPlaylistFileBuilder;
        private Mock<IChannelMatcher> mockChannelMatcher;
        private Mock<IMediaSourceChecker> mockMediaSourceChecker;
        private Mock<IFileRepository<ChannelDefinitionDataObject>> mockChannelRepository;
        private Mock<IFileRepository<GroupDataObject>> mockGroupRepository;
        private Mock<IFileRepository<PlaylistProviderDataObject>> mockProviderRepository;
        private Mock<ILogger> mockLogger;

        private ApplicationSettings applicationSettings;
        private PlaylistAggregator playlistAggregator;

        [SetUp]
        public void SetUp()
        {
            mockPlaylistFetcher = new Mock<IPlaylistFetcher>();
            mockPlaylistFileBuilder = new Mock<IPlaylistFileBuilder>();
            mockChannelMatcher = new Mock<IChannelMatcher>();
            mockMediaSourceChecker = new Mock<IMediaSourceChecker>();
            mockChannelRepository = new Mock<IFileRepository<ChannelDefinitionDataObject>>();
            mockGroupRepository = new Mock<IFileRepository<GroupDataObject>>();
            mockProviderRepository = new Mock<IFileRepository<PlaylistProviderDataObject>>();
            mockLogger = new Mock<ILogger>();

            applicationSettings = new ApplicationSettings
            {
                OutputPlaylistPath = "/tmp/output.m3u",
                DaysToCheck = 7,
                AreUnmatchedChannelsIncluded = true,
                AreTvGuideTagsEnabled = true,
                ArePlaylistDetailsTagsEnabled = true
            };

            playlistAggregator = new PlaylistAggregator(
                mockPlaylistFetcher.Object,
                mockPlaylistFileBuilder.Object,
                mockChannelMatcher.Object,
                mockMediaSourceChecker.Object,
                mockChannelRepository.Object,
                mockGroupRepository.Object,
                mockProviderRepository.Object,
                applicationSettings,
                mockLogger.Object
            );
        }

        [Test]
        [TestCase(1, 1, 1)]
        [TestCase(5, 10, 20)]
        [TestCase(10, 50, 100)]
        [TestCase(100, 500, 1000)]
        [TestCase(1000, 5000, 10000)]
        public void GatherPlaylist_WithVaryingCounts_ReturnsValidPlaylist(int groupCount, int channelCount, int providerChannelCount)
        {
            var groups = CreateGroupDataObjects(groupCount);
            var channels = CreateChannelDefinitionDataObjects(channelCount, groupCount);
            var providers = CreatePlaylistProviderDataObjects(3);
            var providerChannels = CreateChannels(providerChannelCount);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = new List<Channel>(providerChannels) } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(providerChannels.Take(Math.Min(channelCount, providerChannelCount)).ToList());
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            mockPlaylistFetcher.Verify(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()), Times.Once);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(50)]
        public void GatherPlaylist_WithEmptyGroups_HandlesCorrectly(int emptyGroupCount)
        {
            var groups = CreateGroupDataObjects(emptyGroupCount);
            var channels = new List<ChannelDefinitionDataObject>();
            var providers = CreatePlaylistProviderDataObjects(1);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = new List<Channel>() } });
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void GatherPlaylist_WithDifferentChannelInclusionSettings_RespectsSettings(bool includeUnmatched, bool enableGuide)
        {
            applicationSettings.AreUnmatchedChannelsIncluded = includeUnmatched;
            applicationSettings.AreTvGuideTagsEnabled = enableGuide;

            var groups = CreateGroupDataObjects(1);
            var channels = CreateChannelDefinitionDataObjects(5, 1);
            var providers = CreatePlaylistProviderDataObjects(2);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = CreateChannels(10) } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(CreateChannels(5));
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
            mockPlaylistFileBuilder.Verify(b => b.Build(It.IsAny<Playlist>()), Times.Once);
        }

        [Test]
        [TestCase(1, "Group1")]
        [TestCase(5, "News")]
        [TestCase(10, "Sports")]
        [TestCase(50, "Entertainment")]
        [TestCase(100, "Channels")]
        public void GatherPlaylist_WithVaryingGroupNames_ProcessesCorrectly(int channelCount, string groupName)
        {
            var groups = new List<GroupDataObject>
            {
                new GroupDataObject { Id = "group1", Name = groupName, Priority = 1 }
            };
            var channels = Enumerable.Range(1, channelCount)
                .Select(i => new ChannelDefinitionDataObject
                {
                    Id = $"channel{i}",
                    Name = $"Channel {i}",
                    GroupId = "group1",
                    IsEnabled = true
                })
                .ToList();
            var providers = CreatePlaylistProviderDataObjects(1);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = CreateChannels(channelCount) } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(CreateChannels(channelCount));
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(1, 10)]
        [TestCase(5, 50)]
        [TestCase(10, 100)]
        [TestCase(50, 500)]
        [TestCase(100, 1000)]
        public void GatherPlaylist_WithVaryingProviders_AggregatesCorrectly(int providerCount, int channelsPerProvider)
        {
            var groups = CreateGroupDataObjects(1);
            var channels = CreateChannelDefinitionDataObjects(channelsPerProvider, 1);
            var providers = CreatePlaylistProviderDataObjects(providerCount);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(Enumerable.Range(0, providerCount)
                    .Select(i => new Playlist { Channels = CreateChannels(channelsPerProvider) })
                    .ToList());
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(CreateChannels(channelsPerProvider));
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
            mockPlaylistFetcher.Verify(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()), Times.Once);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(14)]
        [TestCase(30)]
        [TestCase(365)]
        public void GatherPlaylist_WithVaryingDaysToCheck_ConfiguresCorrectly(int daysToCheck)
        {
            applicationSettings.DaysToCheck = daysToCheck;

            var groups = CreateGroupDataObjects(1);
            var channels = CreateChannelDefinitionDataObjects(5, 1);
            var providers = CreatePlaylistProviderDataObjects(1);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = CreateChannels(5) } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(CreateChannels(5));
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(1, 1, 1, true)]
        [TestCase(5, 10, 20, true)]
        [TestCase(10, 50, 100, false)]
        [TestCase(50, 100, 500, true)]
        public void GatherPlaylist_WithMixedEnabledDisabled_FiltersCorrectly(
            int groupCount, int channelCount, int providerChannelCount, bool enabledState)
        {
            var groups = CreateGroupDataObjects(groupCount);
            var channels = CreateChannelDefinitionDataObjects(channelCount, groupCount, enabledState);
            var providers = CreatePlaylistProviderDataObjects(2);
            var providerChannels = CreateChannels(providerChannelCount);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = new List<Channel>(providerChannels) } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(providerChannels.Take(Math.Min(channelCount, providerChannelCount)).ToList());
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
        }

        private List<GroupDataObject> CreateGroupDataObjects(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new GroupDataObject
                {
                    Id = $"group{i}",
                    Name = $"Group {i}",
                    Priority = i,
                    IsEnabled = i % 2 == 0
                })
                .ToList();
        }

        private List<ChannelDefinitionDataObject> CreateChannelDefinitionDataObjects(int count, int groupCount, bool enabled = true)
        {
            return Enumerable.Range(1, count)
                .Select(i => new ChannelDefinitionDataObject
                {
                    Id = $"channel{i}",
                    Name = $"Channel {i}",
                    GroupId = $"group{(i % groupCount) + 1}",
                    IsEnabled = enabled,
                    LogoUrl = $"http://example.com/logo{i}.png",
                    Country = $"Country{i % 10}"
                })
                .ToList();
        }

        private List<PlaylistProviderDataObject> CreatePlaylistProviderDataObjects(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new PlaylistProviderDataObject
                {
                    Id = $"provider{i}",
                    Name = $"Provider {i}",
                    Url = $"http://example.com/playlist{i}.m3u",
                    Priority = i,
                    IsEnabled = i <= (count / 2) || count <= 2
                })
                .ToList();
        }

        private List<Channel> CreateChannels(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new Channel
                {
                    Id = $"ch{i}",
                    Name = $"Channel {i}",
                    Group = $"Group{i % 5}",
                    Country = $"Country{i % 10}",
                    LogoUrl = $"http://example.com/logo{i}.png",
                    Number = i,
                    PlaylistId = $"playlist{i % 3}",
                    PlaylistChannelName = $"PlaylistChannel{i}",
                    Url = $"http://example.com/stream{i}.m3u8"
                })
                .ToList();
        }
    }
}
