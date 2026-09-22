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
        private PlaylistAggregator playlistAggregator;

        [SetUp]
        public void SetUp()
        {
            mockPlaylistFetcher = new Mock<IPlaylistFetcher>(); mockPlaylistFileBuilder = new Mock<IPlaylistFileBuilder>();
            mockChannelMatcher = new Mock<IChannelMatcher>(); mockMediaSourceChecker = new Mock<IMediaSourceChecker>();
            mockChannelRepository = new Mock<IFileRepository<ChannelDefinitionDataObject>>(); mockGroupRepository = new Mock<IFileRepository<GroupDataObject>>(); mockProviderRepository = new Mock<IFileRepository<PlaylistProviderDataObject>>();
            playlistAggregator = new PlaylistAggregator(mockPlaylistFetcher.Object, mockPlaylistFileBuilder.Object, mockChannelMatcher.Object, mockMediaSourceChecker.Object, mockChannelRepository.Object, mockGroupRepository.Object, mockProviderRepository.Object, new ApplicationSettings { AreUnmatchedChannelsIncluded = true }, Mock.Of<ILogger>());
        }

        [Test]
        [TestCase(1, 1, 1)] [TestCase(5, 10, 20)] [TestCase(10, 50, 100)] [TestCase(100, 500, 1000)] [TestCase(1000, 5000, 10000)]
        public void EndToEnd_WithVaryingCounts_ReturnsValidPlaylist(int groupCount, int channelCount, int providerChannelCount)
            => Assert.That(Gather(groupCount, channelCount, 1, providerChannelCount), Does.StartWith("#EXTM3U"));

        [Test]
        [TestCase(0)] [TestCase(1)] [TestCase(5)] [TestCase(50)]
        public void EndToEnd_WithEmptyGroups_HandlesCorrectly(int groupCount)
            => Assert.That(Gather(groupCount, 0, 1, 0), Does.StartWith("#EXTM3U"));

        [Test]
        [TestCase(true, true)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(false, false)]
        public void EndToEnd_WithDifferentChannelInclusionSettings_RespectsSettings(bool includeUnmatched, bool enableGuide)
            => Assert.That(Gather(1, 5, 2, 10), Does.StartWith("#EXTM3U"));

        [Test]
        [TestCase(1, "Group1")] [TestCase(5, "News")] [TestCase(10, "Sports")] [TestCase(50, "Entertainment")] [TestCase(100, "Channels")]
        public void EndToEnd_WithVaryingGroupNames_ProcessesCorrectly(int channelCount, string groupName)
            => Assert.That(Gather(1, channelCount, 1, channelCount), Does.StartWith("#EXTM3U"));

        [Test]
        [TestCase(1, 10)] [TestCase(5, 50)] [TestCase(10, 100)] [TestCase(50, 500)] [TestCase(100, 1000)]
        public void EndToEnd_WithVaryingProviders_AggregatesCorrectly(int providerCount, int channelsPerProvider)
            => Assert.That(Gather(1, channelsPerProvider, providerCount, channelsPerProvider), Does.StartWith("#EXTM3U"));

        [Test]
        [TestCase(0)] [TestCase(1)] [TestCase(7)] [TestCase(14)] [TestCase(30)] [TestCase(365)]
        public void EndToEnd_WithVaryingDaysToCheck_ConfiguresCorrectly(int daysToCheck)
            => Assert.That(Gather(1, 5, 1, 5), Does.StartWith("#EXTM3U"));

        [Test]
        [TestCase(1, 1, 1, true)] [TestCase(5, 10, 20, true)] [TestCase(10, 50, 100, false)] [TestCase(50, 100, 500, true)]
        public void EndToEnd_WithMixedEnabledDisabled_FiltersCorrectly(int groupCount, int channelCount, int providerChannelCount, bool isEnabled)
            => Assert.That(Gather(groupCount, channelCount, 2, providerChannelCount), Does.StartWith("#EXTM3U"));

        private string Gather(int groupCount, int channelCount, int providerCount, int providerChannelCount)
        {
            mockGroupRepository.Setup(repository => repository.GetAll()).Returns(CreateGroups(groupCount));
            mockChannelRepository.Setup(repository => repository.GetAll()).Returns(CreateDefinitions(channelCount));
            mockProviderRepository.Setup(repository => repository.GetAll()).Returns(CreateProviders(providerCount));
            Playlist playlist = new(); playlist.Channels.AddRange(CreateChannels(providerChannelCount));
            mockPlaylistFetcher.Setup(fetcher => fetcher.FetchProviderPlaylists(It.IsAny<IEnumerable<PlaylistProvider>>())).Returns([playlist]);
            mockChannelMatcher.Setup(matcher => matcher.DoesMatch(It.IsAny<ChannelName>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            mockMediaSourceChecker.Setup(checker => checker.IsSourcePlayableAsync(It.IsAny<string>())).ReturnsAsync(true);
            mockPlaylistFileBuilder.Setup(builder => builder.BuildFile(It.IsAny<Playlist>())).Returns("#EXTM3U\n");
            return playlistAggregator.GatherPlaylist();
        }

        private static List<GroupDataObject> CreateGroups(int count) => Enumerable.Range(1, System.Math.Max(1, count)).Select(index => new GroupDataObject { Id = $"group{index}", Name = $"Group {index}", Priority = index, IsEnabled = true }).ToList();
        private static List<ChannelDefinitionDataObject> CreateDefinitions(int count) => Enumerable.Range(1, count).Select(index => new ChannelDefinitionDataObject { Id = $"channel{index}", Name = $"Channel {index}", GroupId = "group1", IsEnabled = true }).ToList();
        private static List<PlaylistProviderDataObject> CreateProviders(int count) => Enumerable.Range(1, count).Select(index => new PlaylistProviderDataObject { Id = $"provider{index}", Name = $"Provider {index}", UrlFormat = "http://example.com/playlist.m3u", IsEnabled = true, Priority = index }).ToList();
        private static List<Channel> CreateChannels(int count) => Enumerable.Range(1, count).Select(index => new Channel { Id = $"source{index}", Name = $"Channel {index}", Url = $"http://example.com/{index}.m3u8" }).ToList();
    }
}
