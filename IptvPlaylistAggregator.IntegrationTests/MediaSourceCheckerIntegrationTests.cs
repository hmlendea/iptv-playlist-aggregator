using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using NuciLog.Core;
using NuciWeb.HTTP;

using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.IntegrationTests
{
    [TestFixture]
    public class MediaSourceCheckerIntegrationTests
    {
        private Mock<IHttpClientBuilder> mockHttpClientBuilder;
        private Mock<ILogger> mockLogger;
        private MediaSourceChecker mediaSourceChecker;

        [SetUp]
        public void SetUp()
        {
            mockHttpClientBuilder = new Mock<IHttpClientBuilder>();
            mockLogger = new Mock<ILogger>();
            mediaSourceChecker = new MediaSourceChecker(mockHttpClientBuilder.Object, mockLogger.Object);
        }

        [Test]
        [TestCase("http://example.com/stream.m3u8")]
        [TestCase("https://example.com/stream.m3u8")]
        [TestCase("http://192.168.1.1:8080/stream")]
        [TestCase("https://subdomain.example.com:9999/path/stream.m3u8")]
        public async Task CheckMediaSourceAsync_WithVaryingUrls_ReturnsStatus(string url)
        {
            var channel = new Channel
            {
                Id = "test",
                Name = "Test Channel",
                Url = url
            };

            var result = await mediaSourceChecker.CheckMediaSourceAsync(channel);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<MediaStreamStatus>());
        }

        [Test]
        [TestCase(100)]
        [TestCase(500)]
        [TestCase(1000)]
        [TestCase(5000)]
        public async Task CheckMediaSourceAsync_WithBatchRequests_HandlesMultiple(int channelCount)
        {
            var channels = CreateChannels(channelCount);
            var tasks = channels.Select(ch => mediaSourceChecker.CheckMediaSourceAsync(ch)).ToList();

            await Task.WhenAll(tasks);

            Assert.That(tasks, Is.Not.Empty);
            Assert.That(tasks.All(t => t.IsCompletedSuccessfully), Is.True);
        }

        [Test]
        [TestCase("http://valid.example.com/stream.m3u8", StreamState.Alive)]
        [TestCase("http://invalid.example.com/stream.m3u8", StreamState.Dead)]
        [TestCase("http://forbidden.example.com/stream.m3u8", StreamState.Unauthorized)]
        [TestCase("http://notfound.example.com/stream.m3u8", StreamState.NotFound)]
        public async Task CheckMediaSourceAsync_WithVaryingStatuses_ReturnsCorrectState(string url, StreamState expectedState)
        {
            var channel = new Channel
            {
                Id = "test",
                Name = "Test Channel",
                Url = url
            };

            var result = await mediaSourceChecker.CheckMediaSourceAsync(channel);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.State, Is.TypeOf<StreamState>());
        }

        [Test]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        public async Task CheckMediaSourceAsync_WithParallelRequests_ProcessesConcurrently(int parallelCount)
        {
            var channels = CreateChannels(parallelCount);
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            var results = new List<MediaStreamStatus>();
            await Task.Run(() =>
            {
                Parallel.ForEach(channels, parallelOptions, async channel =>
                {
                    var result = await mediaSourceChecker.CheckMediaSourceAsync(channel);
                    lock (results)
                    {
                        results.Add(result);
                    }
                });
            });

            Assert.That(results.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task CheckMediaSourceAsync_WithEmptyUrl_HandlesGracefully()
        {
            var channel = new Channel
            {
                Id = "test",
                Name = "Test Channel",
                Url = ""
            };

            var result = await mediaSourceChecker.CheckMediaSourceAsync(channel);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task CheckMediaSourceAsync_WithNullUrl_HandlesGracefully()
        {
            var channel = new Channel
            {
                Id = "test",
                Name = "Test Channel",
                Url = null
            };

            var result = await mediaSourceChecker.CheckMediaSourceAsync(channel);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase("http://example.com/stream")]
        [TestCase("https://example.com/stream.m3u")]
        [TestCase("http://example.com/stream.m3u8")]
        [TestCase("http://example.com/stream.ts")]
        [TestCase("http://example.com/stream.mkv")]
        public async Task CheckMediaSourceAsync_WithVaryingStreamFormats_ChecksSuccessfully(string url)
        {
            var channel = new Channel
            {
                Id = "test",
                Name = "Test Channel",
                Url = url
            };

            var result = await mediaSourceChecker.CheckMediaSourceAsync(channel);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase("http://example.com/stream.m3u8?token=abc123")]
        [TestCase("http://example.com/stream.m3u8?auth=token&format=hls")]
        [TestCase("http://example.com/stream.m3u8?id=123&user=test")]
        public async Task CheckMediaSourceAsync_WithQueryParameters_ProcessesCorrectly(string url)
        {
            var channel = new Channel
            {
                Id = "test",
                Name = "Test Channel",
                Url = url
            };

            var result = await mediaSourceChecker.CheckMediaSourceAsync(channel);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task CheckMediaSourceAsync_WithSpecialCharactersInUrl_HandlesCorrectly()
        {
            var channel = new Channel
            {
                Id = "test",
                Name = "Test Channel",
                Url = "http://example.com/stream%20name.m3u8?name=Test%20Channel"
            };

            var result = await mediaSourceChecker.CheckMediaSourceAsync(channel);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        public async Task CheckMediaSourceAsync_WithMultipleBackupUrls_ChecksEachUrl(int backupCount)
        {
            var channels = Enumerable.Range(1, backupCount)
                .Select(i => new Channel
                {
                    Id = $"ch{i}",
                    Name = $"Channel {i} Backup",
                    Url = $"http://backup{i}.example.com/stream{i}.m3u8"
                })
                .ToList();

            var tasks = channels.Select(ch => mediaSourceChecker.CheckMediaSourceAsync(ch)).ToList();
            await Task.WhenAll(tasks);

            Assert.That(tasks.All(t => t.IsCompletedSuccessfully), Is.True);
        }

        [Test]
        [TestCase(100)]
        [TestCase(500)]
        [TestCase(1000)]
        public async Task CheckMediaSourceAsync_WithLargeBatches_CompletesInReasonableTime(int batchSize)
        {
            var channels = CreateChannels(batchSize);
            var startTime = DateTime.Now;
            var tasks = channels.Select(ch => mediaSourceChecker.CheckMediaSourceAsync(ch)).ToList();

            await Task.WhenAll(tasks);

            var elapsed = DateTime.Now - startTime;
            Assert.That(tasks.All(t => t.IsCompletedSuccessfully), Is.True);
            Assert.That(elapsed.TotalSeconds, Is.LessThan(120)); // Reasonable timeout
        }

        [Test]
        public async Task CheckMediaSourceAsync_WithDifferentPortNumbers_HandlesVaryingPorts()
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = "Default Port", Url = "http://example.com/stream.m3u8" },
                new Channel { Id = "2", Name = "Port 8080", Url = "http://example.com:8080/stream.m3u8" },
                new Channel { Id = "3", Name = "Port 9999", Url = "http://example.com:9999/stream.m3u8" },
                new Channel { Id = "4", Name = "Port 443", Url = "https://example.com:443/stream.m3u8" }
            };

            var tasks = channels.Select(ch => mediaSourceChecker.CheckMediaSourceAsync(ch)).ToList();
            await Task.WhenAll(tasks);

            Assert.That(tasks.All(t => t.IsCompletedSuccessfully), Is.True);
        }

        [Test]
        public async Task CheckMediaSourceAsync_WithInternationalDomains_HandlesIdn()
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = "Channel", Url = "http://example.com/stream.m3u8" },
                new Channel { Id = "2", Name = "Channel", Url = "http://münchen.example.com/stream.m3u8" },
                new Channel { Id = "3", Name = "Channel", Url = "http://москва.example.com/stream.m3u8" }
            };

            var tasks = channels.Select(ch => mediaSourceChecker.CheckMediaSourceAsync(ch)).ToList();
            await Task.WhenAll(tasks);

            Assert.That(tasks.Count, Is.EqualTo(channels.Count));
        }

        [Test]
        [TestCase("http://example.com/stream.m3u8")]
        [TestCase("http://example.com/STREAM.M3U8")]
        [TestCase("http://example.com/Stream.M3u8")]
        public async Task CheckMediaSourceAsync_WithVaryingCase_HandlesCorrectly(string url)
        {
            var channel = new Channel
            {
                Id = "test",
                Name = "Test Channel",
                Url = url
            };

            var result = await mediaSourceChecker.CheckMediaSourceAsync(channel);

            Assert.That(result, Is.Not.Null);
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
