using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;
using NuciLog.Core;

using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.IntegrationTests
{
    [TestFixture]
    public class MediaSourceCheckerIntegrationTests
    {
        private Mock<IFileDownloader> mockFileDownloader;
        private Mock<IPlaylistFileBuilder> mockPlaylistFileBuilder;
        private Mock<ICacheManager> mockCacheManager;
        private MediaSourceChecker mediaSourceChecker;

        [SetUp]
        public void SetUp()
        {
            mockFileDownloader = new Mock<IFileDownloader>();
            mockPlaylistFileBuilder = new Mock<IPlaylistFileBuilder>();
            mockCacheManager = new Mock<ICacheManager>();
            mediaSourceChecker = new MediaSourceChecker(mockFileDownloader.Object, mockPlaylistFileBuilder.Object, mockCacheManager.Object, Mock.Of<ILogger>());
            mockCacheManager.Setup(cache => cache.GetStreamStatus(It.IsAny<string>())).Returns((string url) => new MediaStreamStatus { Url = url, State = StreamState.Alive });
        }

        [Test]
        [TestCase("http://example.com/stream.m3u8")] [TestCase("https://example.com/stream.m3u8")]
        [TestCase("http://192.168.1.1:8080/stream")] [TestCase("https://subdomain.example.com:9999/path/stream.m3u8")]
        [TestCase("http://example.com/stream.m3u8?token=abc123")] [TestCase("http://example.com/stream.m3u8?auth=token&format=hls")]
        [TestCase("http://example.com/stream.m3u8?id=123&user=test")] [TestCase("http://example.com/stream%20name.m3u8?name=Test%20Channel")]
        [TestCase("http://example.com/stream")] [TestCase("https://example.com/stream.m3u")]
        [TestCase("http://example.com/stream.ts")] [TestCase("http://example.com/stream.mkv")]
        [TestCase("http://example.com/STREAM.M3U8")] [TestCase("http://example.com/Stream.M3u8")]
        public async Task IsSourcePlayableAsync_WithVaryingUrls_ReturnsCachedStatus(string url)
        {
            bool isPlayable = await mediaSourceChecker.IsSourcePlayableAsync(url);
            Assert.That(isPlayable);
        }

        [Test]
        [TestCase(100)] [TestCase(500)] [TestCase(1000)] [TestCase(5000)]
        public async Task IsSourcePlayableAsync_WithBatchRequests_HandlesMultiple(int urlCount)
        {
            IEnumerable<Task<bool>> tasks = Enumerable.Range(1, urlCount).Select(index => mediaSourceChecker.IsSourcePlayableAsync($"http://example.com/{index}.m3u8"));
            bool[] results = await Task.WhenAll(tasks);
            Assert.That(results, Is.All.True);
        }

        [Test]
        [TestCase(StreamState.Alive, true)] [TestCase(StreamState.Dead, false)] [TestCase(StreamState.Unauthorised, false)]
        [TestCase(StreamState.NotFound, false)] [TestCase(StreamState.Unsupported, false)] [TestCase(StreamState.Blacklisted, false)]
        public async Task IsSourcePlayableAsync_WithVaryingStatuses_ReturnsCorrectResult(StreamState state, bool expectedResult)
        {
            mockCacheManager.Setup(cache => cache.GetStreamStatus("http://example.com/stream.m3u8")).Returns(new MediaStreamStatus { Url = "http://example.com/stream.m3u8", State = state });
            bool isPlayable = await mediaSourceChecker.IsSourcePlayableAsync("http://example.com/stream.m3u8");
            Assert.That(isPlayable, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(1)] [TestCase(5)] [TestCase(10)] [TestCase(50)] [TestCase(100)]
        public async Task IsSourcePlayableAsync_WithParallelRequests_ProcessesConcurrently(int requestCount)
        {
            bool[] results = await Task.WhenAll(Enumerable.Range(1, requestCount).Select(index => mediaSourceChecker.IsSourcePlayableAsync($"http://example.com/{index}.m3u8")));
            Assert.That(results, Is.All.True);
        }

        [Test]
        public async Task IsSourcePlayableAsync_WithEmptyUrl_ReturnsFalse()
        {
            mockCacheManager.Setup(cache => cache.GetStreamStatus(string.Empty)).Returns(new MediaStreamStatus { Url = string.Empty, State = StreamState.Dead });
            Assert.That(await mediaSourceChecker.IsSourcePlayableAsync(string.Empty), Is.False);
        }
    }
}
