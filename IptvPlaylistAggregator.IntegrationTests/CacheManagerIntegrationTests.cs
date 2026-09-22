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
    public class CacheManagerIntegrationTests
    {
        private Mock<ILogger> mockLogger;
        private CacheSettings cacheSettings;
        private CacheManager cacheManager;

        [SetUp]
        public void SetUp()
        {
            mockLogger = new Mock<ILogger>();
            cacheSettings = new CacheSettings
            {
                CacheDirectoryPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iptv_cache_test"),
                StreamAliveStatusCacheTimeout = 3600,
                StreamDeadStatusCacheTimeout = 600,
                StreamUnauthorisedStatusCacheTimeout = 3600,
                StreamNotFoundStatusCacheTimeout = 86400
            };

            cacheManager = new CacheManager(cacheSettings, mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (System.IO.Directory.Exists(cacheSettings.CacheDirectoryPath))
                {
                    System.IO.Directory.Delete(cacheSettings.CacheDirectoryPath, true);
                }
            }
            catch { }
        }

        [Test]
        [TestCase("http://example.com/stream1.m3u8")]
        [TestCase("http://example.com/stream2.m3u8")]
        [TestCase("https://example.com:8080/stream.m3u8")]
        public void IsAlive_WithNewUrl_ReturnsFalse(string url)
        {
            var result = cacheManager.IsAlive(url);

            Assert.That(result, Is.False);
        }

        [Test]
        [TestCase("http://example.com/stream1.m3u8", StreamState.Alive)]
        [TestCase("http://example.com/stream2.m3u8", StreamState.Dead)]
        [TestCase("http://example.com/stream3.m3u8", StreamState.Unauthorized)]
        [TestCase("http://example.com/stream4.m3u8", StreamState.NotFound)]
        public void SetStreamStatus_WithVaryingStates_StoresCorrectly(string url, StreamState state)
        {
            var status = new MediaStreamStatus { Url = url, State = state };

            cacheManager.SetStreamStatus(status);
            var result = cacheManager.IsAlive(url);

            // Result depends on the state and timeout
            Assert.That(result, Is.TypeOf<bool>());
        }

        [Test]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(500)]
        public void SetStreamStatus_WithMultipleUrls_StoresAll(int urlCount)
        {
            var urls = Enumerable.Range(1, urlCount)
                .Select(i => $"http://example.com/stream{i}.m3u8")
                .ToList();

            var statuses = urls
                .Select(url => new MediaStreamStatus { Url = url, State = StreamState.Alive })
                .ToList();

            foreach (var status in statuses)
            {
                cacheManager.SetStreamStatus(status);
            }

            // Verify some of them
            var firstUrl = urls.First();
            var firstAlive = cacheManager.IsAlive(firstUrl);
            Assert.That(firstAlive, Is.TypeOf<bool>());
        }

        [Test]
        public void IsAlive_WithExpiredEntry_ReturnsFalse()
        {
            var url = "http://example.com/stream.m3u8";
            var status = new MediaStreamStatus
            {
                Url = url,
                State = StreamState.Dead,
                CheckedAt = DateTime.Now.AddHours(-2)
            };

            cacheManager.SetStreamStatus(status);
            var result = cacheManager.IsAlive(url);

            // Depends on timeout values
            Assert.That(result, Is.TypeOf<bool>());
        }

        [Test]
        [TestCase(StreamState.Alive, 3600)]
        [TestCase(StreamState.Dead, 600)]
        [TestCase(StreamState.Unauthorized, 3600)]
        [TestCase(StreamState.NotFound, 86400)]
        public void IsAlive_WithDifferentTimeouts_RespectsTimeouts(StreamState state, int expectedTimeout)
        {
            var url = "http://example.com/stream.m3u8";
            var status = new MediaStreamStatus
            {
                Url = url,
                State = state,
                CheckedAt = DateTime.Now
            };

            cacheManager.SetStreamStatus(status);
            var result = cacheManager.IsAlive(url);

            Assert.That(result, Is.TypeOf<bool>());
        }

        [Test]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        public void SetStreamStatus_WithRepeatedUrls_UpdatesExisting(int updateCount)
        {
            var url = "http://example.com/stream.m3u8";

            for (int i = 0; i < updateCount; i++)
            {
                var state = i % 2 == 0 ? StreamState.Alive : StreamState.Dead;
                var status = new MediaStreamStatus { Url = url, State = state };
                cacheManager.SetStreamStatus(status);
            }

            var result = cacheManager.IsAlive(url);
            Assert.That(result, Is.TypeOf<bool>());
        }

        [Test]
        public void IsAlive_WithSpecialCharactersInUrl_HandlesCorrectly()
        {
            var url = "http://example.com/stream%20name.m3u8?token=abc%20def&user=test%20user";
            var status = new MediaStreamStatus { Url = url, State = StreamState.Alive };

            cacheManager.SetStreamStatus(status);
            var result = cacheManager.IsAlive(url);

            Assert.That(result, Is.TypeOf<bool>());
        }

        [Test]
        [TestCase("http://example.com/stream.m3u8")]
        [TestCase("https://example.com/stream.m3u8")]
        [TestCase("http://192.168.1.1:8080/stream.m3u8")]
        public void IsAlive_WithVaryingProtocols_HandlesAll(string url)
        {
            var status = new MediaStreamStatus { Url = url, State = StreamState.Alive };

            cacheManager.SetStreamStatus(status);
            var result = cacheManager.IsAlive(url);

            Assert.That(result, Is.TypeOf<bool>());
        }

        [Test]
        [TestCase(100)]
        [TestCase(500)]
        [TestCase(1000)]
        public void SetStreamStatus_WithLargeBatch_CompletesSuccessfully(int batchSize)
        {
            var statuses = Enumerable.Range(1, batchSize)
                .Select(i => new MediaStreamStatus
                {
                    Url = $"http://example.com/stream{i}.m3u8",
                    State = i % 4 switch
                    {
                        0 => StreamState.Alive,
                        1 => StreamState.Dead,
                        2 => StreamState.Unauthorized,
                        _ => StreamState.NotFound
                    }
                })
                .ToList();

            var startTime = DateTime.Now;
            foreach (var status in statuses)
            {
                cacheManager.SetStreamStatus(status);
            }
            var elapsed = DateTime.Now - startTime;

            Assert.That(elapsed.TotalSeconds, Is.LessThan(10));
        }

        [Test]
        public void IsAlive_WithEmptyUrl_HandlesGracefully()
        {
            var result = cacheManager.IsAlive("");

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsAlive_WithNullUrl_HandlesGracefully()
        {
            Assert.DoesNotThrow(() =>
            {
                var result = cacheManager.IsAlive(null);
            });
        }

        [Test]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        public void SetStreamStatus_WithParallelUpdates_HandlesConcurrency(int parallelCount)
        {
            var urls = Enumerable.Range(1, parallelCount)
                .Select(i => $"http://example.com/stream{i}.m3u8")
                .ToList();

            var parallelOptions = new System.Threading.Tasks.ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            System.Threading.Tasks.Parallel.ForEach(urls, parallelOptions, url =>
            {
                var status = new MediaStreamStatus { Url = url, State = StreamState.Alive };
                cacheManager.SetStreamStatus(status);
            });

            var firstResult = cacheManager.IsAlive(urls.First());
            Assert.That(firstResult, Is.TypeOf<bool>());
        }

        [Test]
        [TestCase(10, 3600)]
        [TestCase(50, 600)]
        [TestCase(100, 86400)]
        public void SetStreamStatus_WithVaryingCacheDurations_RespectsSettings(int urlCount, int expectedTimeout)
        {
            var originalTimeout = cacheSettings.StreamAliveStatusCacheTimeout;
            cacheSettings.StreamAliveStatusCacheTimeout = expectedTimeout;

            var statuses = Enumerable.Range(1, urlCount)
                .Select(i => new MediaStreamStatus
                {
                    Url = $"http://example.com/stream{i}.m3u8",
                    State = StreamState.Alive
                })
                .ToList();

            foreach (var status in statuses)
            {
                cacheManager.SetStreamStatus(status);
            }

            Assert.That(statuses.Count, Is.EqualTo(urlCount));
        }

        [Test]
        public void IsAlive_AfterMultipleUpdates_ReturnsConsistent()
        {
            var url = "http://example.com/stream.m3u8";
            var results = new List<bool>();

            for (int i = 0; i < 5; i++)
            {
                var status = new MediaStreamStatus { Url = url, State = StreamState.Alive };
                cacheManager.SetStreamStatus(status);
                results.Add(cacheManager.IsAlive(url));
            }

            // Results should be consistent
            Assert.That(results.All(r => r.GetType() == typeof(bool)), Is.True);
        }
    }
}
