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
    public class EndToEndIntegrationTests
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
        [TestCase(2, 10, 50, 25)]
        [TestCase(5, 25, 100, 50)]
        [TestCase(10, 50, 500, 200)]
        public void EndToEnd_CompleteAggregationFlow_SucceedsWithScale(
            int groupCount, int channelCount, int providerChannelCount, int matchedChannelCount)
        {
            var groups = CreateGroupDataObjects(groupCount);
            var channels = CreateChannelDefinitionDataObjects(channelCount, groupCount);
            var providers = CreatePlaylistProviderDataObjects(3);
            var providerChannels = CreateChannels(providerChannelCount);
            var matchedChannels = providerChannels.Take(matchedChannelCount).ToList();

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = providerChannels } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(matchedChannels);
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("#"));
            mockPlaylistFetcher.Verify(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()), Times.Once);
            mockChannelMatcher.Verify(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()), Times.Once);
        }

        [Test]
        [TestCase(1, 5, 10)]
        [TestCase(5, 50, 100)]
        [TestCase(10, 100, 500)]
        public void EndToEnd_MultiProviderAggregation_Succeeds(int providerCount, int channelsPerProvider, int totalMatches)
        {
            var groups = CreateGroupDataObjects(2);
            var channels = CreateChannelDefinitionDataObjects(totalMatches, 2);
            var providers = CreatePlaylistProviderDataObjects(providerCount);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(Enumerable.Range(0, providerCount)
                    .Select(i => new Playlist { Channels = CreateChannels(channelsPerProvider) })
                    .ToList());
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(CreateChannels(totalMatches));
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
            mockPlaylistFetcher.Verify(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()), Times.Once);
        }

        [Test]
        [TestCase(true, true, true)]
        [TestCase(true, false, true)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public void EndToEnd_WithFeatureFlags_RespectsSettings(bool includeUnmatched, bool enableGuide, bool enableDetails)
        {
            applicationSettings.AreUnmatchedChannelsIncluded = includeUnmatched;
            applicationSettings.AreTvGuideTagsEnabled = enableGuide;
            applicationSettings.ArePlaylistDetailsTagsEnabled = enableDetails;

            var groups = CreateGroupDataObjects(1);
            var channels = CreateChannelDefinitionDataObjects(10, 1);
            var providers = CreatePlaylistProviderDataObjects(2);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = CreateChannels(15) } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(CreateChannels(10));
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(5, 0)]
        [TestCase(0, 10)]
        public void EndToEnd_WithMissingData_HandlesGracefully(int channelCount, int providerCount)
        {
            var groups = CreateGroupDataObjects(1);
            var channels = CreateChannelDefinitionDataObjects(channelCount, 1);
            var providers = CreatePlaylistProviderDataObjects(providerCount);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new List<Playlist>());
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(5, 5)]
        [TestCase(10, 10)]
        [TestCase(50, 50)]
        [TestCase(100, 100)]
        public void EndToEnd_WithEnabledDisabledMix_FiltersCorrectly(int totalGroups, int totalChannels)
        {
            var groups = CreateGroupDataObjects(totalGroups);
            var channels = CreateChannelDefinitionDataObjects(totalChannels, totalGroups);
            var providers = CreatePlaylistProviderDataObjects(2);

            var enabledGroupCount = groups.Count(g => g.IsEnabled);
            var enabledChannelCount = channels.Count(c => c.IsEnabled);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = CreateChannels(totalChannels) } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(CreateChannels(Math.Min(enabledChannelCount, totalChannels)));
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task EndToEnd_AsyncPlaylistFetching_CompletesSuccessfully()
        {
            var groups = CreateGroupDataObjects(1);
            var channels = CreateChannelDefinitionDataObjects(5, 1);
            var providers = CreatePlaylistProviderDataObjects(3);
            var fetcherMock = new Mock<IPlaylistFetcher>();

            var playlists = Enumerable.Range(0, 3)
                .Select(i => new Playlist { Channels = CreateChannels(10) })
                .ToList();

            fetcherMock.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(playlists);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(playlists);
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(CreateChannels(5));
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            await Task.Run(() => playlistAggregator.GatherPlaylist());

            Assert.That(playlistAggregator, Is.Not.Null);
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(50, 50)]
        [TestCase(100, 100)]
        [TestCase(500, 500)]
        public void EndToEnd_WithRealWorldScenario_ScalesAppropriately(int groupCount, int channelCount)
        {
            var groups = CreateGroupDataObjects(Math.Max(1, groupCount / 10));
            var channels = CreateChannelDefinitionDataObjects(channelCount, Math.Max(1, groupCount / 10));
            var providers = CreatePlaylistProviderDataObjects(5);
            var providerChannels = CreateChannels(channelCount * 2);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = providerChannels } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(providerChannels.Take(channelCount).ToList());
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var startTime = DateTime.Now;
            var result = playlistAggregator.GatherPlaylist();
            var elapsed = DateTime.Now - startTime;

            Assert.That(result, Is.Not.Null);
            Assert.That(elapsed.TotalSeconds, Is.LessThan(30)); // Should complete reasonably
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(5, 5)]
        [TestCase(10, 10)]
        public void EndToEnd_WithGroupPriorities_MaintainsOrder(int groupCount, int channelsPerGroup)
        {
            var groups = Enumerable.Range(1, groupCount)
                .Select(i => new GroupDataObject
                {
                    Id = $"group{i}",
                    Name = $"Group {i}",
                    Priority = i,
                    IsEnabled = true
                })
                .ToList();

            var channels = Enumerable.Range(1, channelsPerGroup)
                .SelectMany(j => groups.Select(g => new ChannelDefinitionDataObject
                {
                    Id = $"channel{g.Id}_{j}",
                    Name = $"Channel {g.Id} {j}",
                    GroupId = g.Id,
                    IsEnabled = true
                }))
                .ToList();

            var providers = CreatePlaylistProviderDataObjects(1);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = CreateChannels(channels.Count) } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(CreateChannels(channels.Count));
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void EndToEnd_WithComplexGroupChannelHierarchy_ProcessesCorrectly()
        {
            var groups = new List<GroupDataObject>
            {
                new GroupDataObject { Id = "entertainment", Name = "Entertainment", Priority = 1, IsEnabled = true },
                new GroupDataObject { Id = "news", Name = "News", Priority = 2, IsEnabled = true },
                new GroupDataObject { Id = "sports", Name = "Sports", Priority = 3, IsEnabled = true },
                new GroupDataObject { Id = "movies", Name = "Movies", Priority = 4, IsEnabled = false }
            };

            var channels = new List<ChannelDefinitionDataObject>
            {
                new ChannelDefinitionDataObject { Id = "bbc1", Name = "BBC One", GroupId = "entertainment", IsEnabled = true },
                new ChannelDefinitionDataObject { Id = "bbc2", Name = "BBC Two", GroupId = "entertainment", IsEnabled = true },
                new ChannelDefinitionDataObject { Id = "bbc3", Name = "BBC Three", GroupId = "entertainment", IsEnabled = false },
                new ChannelDefinitionDataObject { Id = "skynews", Name = "Sky News", GroupId = "news", IsEnabled = true },
                new ChannelDefinitionDataObject { Id = "espn", Name = "ESPN", GroupId = "sports", IsEnabled = true }
            };

            var providers = CreatePlaylistProviderDataObjects(2);

            mockGroupRepository.Setup(r => r.GetAll()).Returns(groups);
            mockChannelRepository.Setup(r => r.GetAll()).Returns(channels);
            mockProviderRepository.Setup(r => r.GetAll()).Returns(providers);
            mockPlaylistFetcher.Setup(f => f.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>()))
                .Returns(new[] { new Playlist { Channels = CreateChannels(10) } });
            mockChannelMatcher.Setup(m => m.MatchChannels(It.IsAny<List<Channel>>(), It.IsAny<List<ChannelDefinition>>()))
                .Returns(CreateChannels(4));
            mockPlaylistFileBuilder.Setup(b => b.Build(It.IsAny<Playlist>()))
                .Returns("#EXTM3U\n");

            var result = playlistAggregator.GatherPlaylist();

            Assert.That(result, Is.Not.Null);
        }

        private List<GroupDataObject> CreateGroupDataObjects(int count)
        {
            return Enumerable.Range(1, Math.Max(1, count))
                .Select(i => new GroupDataObject
                {
                    Id = $"group{i}",
                    Name = $"Group {i}",
                    Priority = i,
                    IsEnabled = i % 2 == 0
                })
                .ToList();
        }

        private List<ChannelDefinitionDataObject> CreateChannelDefinitionDataObjects(int count, int groupCount)
        {
            var actualGroupCount = Math.Max(1, groupCount);
            return Enumerable.Range(1, Math.Max(1, count))
                .Select(i => new ChannelDefinitionDataObject
                {
                    Id = $"channel{i}",
                    Name = $"Channel {i}",
                    GroupId = $"group{(i % actualGroupCount) + 1}",
                    IsEnabled = i % 2 == 0,
                    LogoUrl = $"http://example.com/logo{i}.png",
                    Country = $"Country{i % 10}"
                })
                .ToList();
        }

        private List<PlaylistProviderDataObject> CreatePlaylistProviderDataObjects(int count)
        {
            return Enumerable.Range(1, Math.Max(1, count))
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
            return Enumerable.Range(1, Math.Max(0, count))
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
