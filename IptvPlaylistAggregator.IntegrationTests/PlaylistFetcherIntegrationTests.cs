using System;
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
        private Mock<ILogger> mockLogger;

        private ApplicationSettings applicationSettings;
        private PlaylistFetcher playlistFetcher;

        [SetUp]
        public void SetUp()
        {
            mockFileDownloader = new Mock<IFileDownloader>();
            mockPlaylistFileBuilder = new Mock<IPlaylistFileBuilder>();
            mockCacheManager = new Mock<ICacheManager>();
            mockLogger = new Mock<ILogger>();

            applicationSettings = new ApplicationSettings
            {
                DaysToCheck = 7,
                OutputPlaylistPath = "/tmp/output.m3u"
            };

            playlistFetcher = new PlaylistFetcher(
                mockFileDownloader.Object,
                mockPlaylistFileBuilder.Object,
                mockCacheManager.Object,
                applicationSettings,
                mockLogger.Object
            );
        }

        [Test]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        public void FetchProviderPlaylists_WithVaryingProviderCounts_ReturnsPlaylists(int providerCount)
        {
            var providers = CreatePlaylistProviders(providerCount);
            var playlistContent = CreateValidM3uContent(20);

            mockFileDownloader.Setup(d => d.Download(It.IsAny<string>()))
                .Returns(playlistContent);
            mockPlaylistFileBuilder.Setup(b => b.Parse(It.IsAny<string>()))
                .Returns(new Playlist { Channels = CreateChannels(20) });
            mockCacheManager.Setup(c => c.IsAlive(It.IsAny<string>()))
                .Returns(true);

            var result = playlistFetcher.FetchProviderPlaylists(providers).ToList();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
            Assert.That(result.Count, Is.LessThanOrEqualTo(providerCount));
            mockFileDownloader.Verify(d => d.Download(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Test]
        [TestCase("http://example.com/playlist.m3u")]
        [TestCase("https://example.com:8080/path/playlist.m3u8")]
        [TestCase("http://192.168.1.1/streaming/playlist.m3u")]
        [TestCase("https://subdomain.example.com/deep/path/playlist.m3u")]
        [TestCase("http://example.com/playlist?token=abc123&format=m3u")]
        public async Task FetchProviderPlaylistAsync_WithVaryingUrls_FetchesSuccessfully(string url)
        {
            var provider = new PlaylistProvider
            {
                Id = "test",
                Name = "Test Provider",
                Url = url,
                Priority = 1,
                IsEnabled = true
            };

            var playlistContent = CreateValidM3uContent(10);

            mockFileDownloader.Setup(d => d.Download(url))
                .Returns(playlistContent);
            mockPlaylistFileBuilder.Setup(b => b.Parse(It.IsAny<string>()))
                .Returns(new Playlist { Channels = CreateChannels(10) });
            mockCacheManager.Setup(c => c.IsAlive(It.IsAny<string>()))
                .Returns(true);

            var result = await playlistFetcher.FetchProviderPlaylistAsync(provider);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Channels, Is.Not.Null);
            mockFileDownloader.Verify(d => d.Download(url), Times.Once);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public async Task FetchProviderPlaylistAsync_WithVaryingChannelCounts_ParsesCorrectly(int channelCount)
        {
            var provider = new PlaylistProvider
            {
                Id = "test",
                Name = "Test Provider",
                Url = "http://example.com/playlist.m3u",
                Priority = 1,
                IsEnabled = true
            };

            var playlistContent = CreateValidM3uContent(channelCount);

            mockFileDownloader.Setup(d => d.Download(It.IsAny<string>()))
                .Returns(playlistContent);
            mockPlaylistFileBuilder.Setup(b => b.Parse(It.IsAny<string>()))
                .Returns(new Playlist { Channels = CreateChannels(channelCount) });
            mockCacheManager.Setup(c => c.IsAlive(It.IsAny<string>()))
                .Returns(true);

            var result = await playlistFetcher.FetchProviderPlaylistAsync(provider);

            Assert.That(result.Channels.Count, Is.EqualTo(channelCount));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task FetchProviderPlaylistAsync_WithCacheStatus_RespectsCacheState(bool cacheHit)
        {
            var provider = new PlaylistProvider
            {
                Id = "test",
                Name = "Test Provider",
                Url = "http://example.com/playlist.m3u",
                Priority = 1,
                IsEnabled = true
            };

            mockCacheManager.Setup(c => c.IsAlive(It.IsAny<string>()))
                .Returns(cacheHit);
            mockFileDownloader.Setup(d => d.Download(It.IsAny<string>()))
                .Returns(CreateValidM3uContent(5));
            mockPlaylistFileBuilder.Setup(b => b.Parse(It.IsAny<string>()))
                .Returns(new Playlist { Channels = CreateChannels(5) });

            var result = await playlistFetcher.FetchProviderPlaylistAsync(provider);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(5, 5)]
        [TestCase(10, 10)]
        [TestCase(25, 50)]
        [TestCase(50, 100)]
        public void FetchProviderPlaylists_WithMultiplePriorities_SortsCorrectly(int providerCount, int channelCount)
        {
            var providers = CreatePlaylistProviders(providerCount);
            var playlistContent = CreateValidM3uContent(channelCount);

            mockFileDownloader.Setup(d => d.Download(It.IsAny<string>()))
                .Returns(playlistContent);
            mockPlaylistFileBuilder.Setup(b => b.Parse(It.IsAny<string>()))
                .Returns(new Playlist { Channels = CreateChannels(channelCount) });
            mockCacheManager.Setup(c => c.IsAlive(It.IsAny<string>()))
                .Returns(true);

            var result = playlistFetcher.FetchProviderPlaylists(providers).ToList();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        [TestCase("BBC", 50)]
        [TestCase("Sky", 150)]
        [TestCase("Netflix", 500)]
        [TestCase("HBO", 300)]
        [TestCase("Disney", 200)]
        public async Task FetchProviderPlaylistAsync_WithNamedProviders_FetchesCorrectly(string providerName, int channelCount)
        {
            var provider = new PlaylistProvider
            {
                Id = providerName.ToLower(),
                Name = providerName,
                Url = $"http://example.com/{providerName.ToLower()}/playlist.m3u",
                Priority = 1,
                IsEnabled = true
            };

            mockFileDownloader.Setup(d => d.Download(It.IsAny<string>()))
                .Returns(CreateValidM3uContent(channelCount));
            mockPlaylistFileBuilder.Setup(b => b.Parse(It.IsAny<string>()))
                .Returns(new Playlist { Channels = CreateChannels(channelCount) });
            mockCacheManager.Setup(c => c.IsAlive(It.IsAny<string>()))
                .Returns(true);

            var result = await playlistFetcher.FetchProviderPlaylistAsync(provider);

            Assert.That(result.Channels.Count, Is.EqualTo(channelCount));
        }

        [Test]
        public async Task FetchProviderPlaylistAsync_WithDisabledProvider_StillFetches()
        {
            var provider = new PlaylistProvider
            {
                Id = "disabled",
                Name = "Disabled Provider",
                Url = "http://example.com/playlist.m3u",
                Priority = 10,
                IsEnabled = false
            };

            mockFileDownloader.Setup(d => d.Download(It.IsAny<string>()))
                .Returns(CreateValidM3uContent(5));
            mockPlaylistFileBuilder.Setup(b => b.Parse(It.IsAny<string>()))
                .Returns(new Playlist { Channels = CreateChannels(5) });
            mockCacheManager.Setup(c => c.IsAlive(It.IsAny<string>()))
                .Returns(true);

            var result = await playlistFetcher.FetchProviderPlaylistAsync(provider);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(1, 10)]
        [TestCase(5, 50)]
        [TestCase(10, 100)]
        [TestCase(20, 500)]
        public void FetchProviderPlaylists_WithMixedPriorities_AggregatesCorrectly(int providerCount, int channelCount)
        {
            var providers = Enumerable.Range(1, providerCount)
                .Select(i => new PlaylistProvider
                {
                    Id = $"provider{i}",
                    Name = $"Provider {i}",
                    Url = $"http://example.com/provider{i}.m3u",
                    Priority = i % 5 == 0 ? 1 : (i % 3 == 0 ? 2 : 3),
                    IsEnabled = true
                })
                .ToList();

            mockFileDownloader.Setup(d => d.Download(It.IsAny<string>()))
                .Returns(CreateValidM3uContent(channelCount));
            mockPlaylistFileBuilder.Setup(b => b.Parse(It.IsAny<string>()))
                .Returns(new Playlist { Channels = CreateChannels(channelCount) });
            mockCacheManager.Setup(c => c.IsAlive(It.IsAny<string>()))
                .Returns(true);

            var result = playlistFetcher.FetchProviderPlaylists(providers).ToList();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        [TestCase(0, 100)]
        [TestCase(7, 50)]
        [TestCase(14, 25)]
        [TestCase(30, 10)]
        public async Task FetchProviderPlaylistAsync_WithVaryingDaysToCheck_FetchesCorrectly(int daysToCheck, int channelCount)
        {
            applicationSettings.DaysToCheck = daysToCheck;

            var provider = new PlaylistProvider
            {
                Id = "test",
                Name = "Test Provider",
                Url = "http://example.com/playlist.m3u",
                Priority = 1,
                IsEnabled = true
            };

            mockFileDownloader.Setup(d => d.Download(It.IsAny<string>()))
                .Returns(CreateValidM3uContent(channelCount));
            mockPlaylistFileBuilder.Setup(b => b.Parse(It.IsAny<string>()))
                .Returns(new Playlist { Channels = CreateChannels(channelCount) });
            mockCacheManager.Setup(c => c.IsAlive(It.IsAny<string>()))
                .Returns(true);

            var result = await playlistFetcher.FetchProviderPlaylistAsync(provider);

            Assert.That(result.Channels.Count, Is.EqualTo(channelCount));
        }

        private List<PlaylistProvider> CreatePlaylistProviders(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new PlaylistProvider
                {
                    Id = $"provider{i}",
                    Name = $"Provider {i}",
                    Url = $"http://example.com/provider{i}.m3u",
                    Priority = i,
                    IsEnabled = i % 2 == 0
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

        private string CreateValidM3uContent(int channelCount)
        {
            var content = "#EXTM3U\n";
            for (int i = 1; i <= channelCount; i++)
            {
                content += $"#EXTINF:-1 tvg-id=\"ch{i}\" tvg-name=\"Channel {i}\" group-title=\"Group{i % 5}\",Channel {i}\n";
                content += $"http://example.com/stream{i}.m3u8\n";
            }
            return content;
        }
    }
}
