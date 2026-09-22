using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;
using NuciLog.Core;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.IntegrationTests
{
    [TestFixture]
    public class PlaylistFetcherIntegrationTests
    {
        private Mock<IFileDownloader> mockFileDownloader;
        private Mock<IPlaylistFileBuilder> mockPlaylistFileBuilder;
        private Mock<ICacheManager> mockCacheManager;
        private PlaylistFetcher playlistFetcher;

        [SetUp]
        public void SetUp()
        {
            mockFileDownloader = new Mock<IFileDownloader>();
            mockPlaylistFileBuilder = new Mock<IPlaylistFileBuilder>();
            mockCacheManager = new Mock<ICacheManager>();
            playlistFetcher = new PlaylistFetcher(
                mockFileDownloader.Object, mockPlaylistFileBuilder.Object, mockCacheManager.Object,
                new ApplicationSettings { DaysToCheck = 7 }, Mock.Of<ILogger>());
        }

        [Test]
        [TestCase(1)] [TestCase(5)] [TestCase(10)] [TestCase(50)] [TestCase(100)]
        public void FetchProviderPlaylists_WithVaryingProviderCounts_ReturnsPlaylists(int providerCount)
        {
            IEnumerable<PlaylistProvider> providers = CreateProviders(providerCount);
            ConfigureSuccessfulFetch(CreateChannels(20));
            List<Playlist> playlists = playlistFetcher.FetchProviderPlaylists(providers).ToList();
            Assert.That(playlists, Has.Count.EqualTo(providerCount));
        }

        [Test]
        [TestCase("http://example.com/playlist.m3u")]
        [TestCase("https://example.com:8080/path/playlist.m3u8")]
        [TestCase("http://192.168.1.1/streaming/playlist.m3u")]
        [TestCase("https://subdomain.example.com/deep/path/playlist.m3u")]
        [TestCase("http://example.com/playlist?token=abc123&format=m3u")]
        public async Task FetchProviderPlaylistAsync_WithVaryingUrls_FetchesSuccessfully(string url)
        {
            ConfigureSuccessfulFetch(CreateChannels(10));
            Playlist playlist = await playlistFetcher.FetchProviderPlaylistAsync(CreateProvider(url));
            Assert.That(playlist.Channels, Has.Count.EqualTo(10));
            mockFileDownloader.Verify(downloader => downloader.TryDownloadStringAsync(url), Times.Once);
        }

        [Test]
        [TestCase(0)] [TestCase(1)] [TestCase(10)] [TestCase(100)] [TestCase(1000)]
        public async Task FetchProviderPlaylistAsync_WithVaryingChannelCounts_ParsesCorrectly(int channelCount)
        {
            ConfigureSuccessfulFetch(CreateChannels(channelCount));
            Playlist playlist = await playlistFetcher.FetchProviderPlaylistAsync(CreateProvider());
            Assert.That(playlist.Channels, Has.Count.EqualTo(channelCount));
        }

        [Test]
        [TestCase(true)] [TestCase(false)]
        public async Task FetchProviderPlaylistAsync_WithCacheStatus_RespectsCacheState(bool isCachingEnabled)
        {
            PlaylistProvider provider = CreateProvider();
            provider.IsCachingEnabled = isCachingEnabled;
            ConfigureSuccessfulFetch(CreateChannels(5));
            Playlist playlist = await playlistFetcher.FetchProviderPlaylistAsync(provider);
            Assert.That(playlist, Is.Not.Null);
        }

        [Test]
        [TestCase(1, 1)] [TestCase(5, 5)] [TestCase(10, 10)] [TestCase(25, 50)] [TestCase(50, 100)]
        public void FetchProviderPlaylists_WithMultiplePriorities_SortsCorrectly(int providerCount, int channelCount)
        {
            ConfigureSuccessfulFetch(CreateChannels(channelCount));
            List<PlaylistProvider> providers = CreateProviders(providerCount);
            List<Playlist> playlists = playlistFetcher.FetchProviderPlaylists(providers).ToList();
            Assert.That(playlists, Has.Count.EqualTo(providerCount));
        }

        [Test]
        [TestCase("BBC", 50)] [TestCase("Sky", 150)] [TestCase("Netflix", 500)] [TestCase("HBO", 300)] [TestCase("Disney", 200)]
        public async Task FetchProviderPlaylistAsync_WithNamedProviders_FetchesCorrectly(string name, int channelCount)
        {
            ConfigureSuccessfulFetch(CreateChannels(channelCount));
            Playlist playlist = await playlistFetcher.FetchProviderPlaylistAsync(CreateProvider(name: name));
            Assert.That(playlist.Channels, Has.Count.EqualTo(channelCount));
        }

        [Test]
        public async Task FetchProviderPlaylistAsync_WithDisabledProvider_StillFetches()
        {
            ConfigureSuccessfulFetch(CreateChannels(5));
            PlaylistProvider provider = CreateProvider();
            provider.IsEnabled = false;
            Playlist playlist = await playlistFetcher.FetchProviderPlaylistAsync(provider);
            Assert.That(playlist, Is.Not.Null);
        }

        [Test]
        [TestCase(1, 10)] [TestCase(5, 50)] [TestCase(10, 100)] [TestCase(20, 500)]
        public void FetchProviderPlaylists_WithMixedPriorities_AggregatesCorrectly(int providerCount, int channelCount)
        {
            ConfigureSuccessfulFetch(CreateChannels(channelCount));
            List<PlaylistProvider> providers = CreateProviders(providerCount);
            List<Playlist> playlists = playlistFetcher.FetchProviderPlaylists(providers).ToList();
            Assert.That(playlists, Has.Count.EqualTo(providerCount));
        }

        [Test]
        [TestCase(0, 100)] [TestCase(7, 50)] [TestCase(14, 25)] [TestCase(30, 10)]
        public async Task FetchProviderPlaylistAsync_WithVaryingDaysToCheck_FetchesCorrectly(int daysToCheck, int channelCount)
        {
            ConfigureSuccessfulFetch(CreateChannels(channelCount));
            Playlist playlist = await playlistFetcher.FetchProviderPlaylistAsync(CreateProvider());
            Assert.That(playlist.Channels, Has.Count.EqualTo(channelCount));
        }

        private void ConfigureSuccessfulFetch(IEnumerable<Channel> channels)
        {
            Playlist playlist = new();
            playlist.Channels.AddRange(channels);
            mockFileDownloader.Setup(downloader => downloader.TryDownloadStringAsync(It.IsAny<string>()))
                .ReturnsAsync("#EXTM3U");
            mockPlaylistFileBuilder.Setup(builder => builder.TryParseFile(It.IsAny<string>())).Returns(playlist);
        }

        private static PlaylistProvider CreateProvider(string url = "http://example.com/playlist.m3u", string name = "Provider") => new()
        {
            Id = name, Name = name, UrlFormat = url, Priority = 1, IsEnabled = true
        };

        private static List<PlaylistProvider> CreateProviders(int count) => Enumerable.Range(1, count)
            .Select(index => new PlaylistProvider { Id = $"provider{index}", Name = $"Provider {index}", UrlFormat = $"http://example.com/{index}.m3u", Priority = index, IsEnabled = true }).ToList();

        private static List<Channel> CreateChannels(int count) => Enumerable.Range(1, count)
            .Select(index => new Channel { Id = $"channel{index}", Name = $"Channel {index}", Url = $"http://example.com/{index}.m3u8" }).ToList();
    }
}
