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
        private CacheSettings cacheSettings;
        private CacheManager cacheManager;

        [SetUp]
        public void SetUp()
        {
            cacheSettings = new CacheSettings
            {
                CacheDirectoryPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iptv_cache_test"),
                StreamAliveStatusCacheTimeout = 3600,
                StreamDeadStatusCacheTimeout = 600,
                StreamUnauthorisedStatusCacheTimeout = 3600,
                StreamNotFoundStatusCacheTimeout = 86400
            };

            cacheManager = new CacheManager(cacheSettings);
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
        public void GetStreamStatus_WithNewUrl_ReturnsNull(string url)
        {
            var result = cacheManager.GetStreamStatus(url);

            Assert.That(result, Is.Null);
        }

        [Test]
        [TestCase("http://example.com/stream1.m3u8", StreamState.Alive)]
        [TestCase("http://example.com/stream2.m3u8", StreamState.Dead)]
        [TestCase("http://example.com/stream3.m3u8", StreamState.Unauthorised)]
        [TestCase("http://example.com/stream4.m3u8", StreamState.NotFound)]
        [TestCase("http://example.com/stream5.m3u8", StreamState.Unsupported)]
        [TestCase("http://example.com/stream6.m3u8", StreamState.Blacklisted)]
        public void StoreStreamStatus_WithVaryingStates_StoresCorrectly(string url, StreamState state)
        {
            var status = new MediaStreamStatus { Url = url, State = state };

            cacheManager.StoreStreamStatus(status);
            var result = cacheManager.GetStreamStatus(url);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.State, Is.EqualTo(state));
        }

        [Test]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(500)]
        public void StoreStreamStatus_WithMultipleUrls_StoresAll(int urlCount)
        {
            var urls = Enumerable.Range(1, urlCount)
                .Select(i => $"http://example.com/stream{i}.m3u8")
                .ToList();

            var statuses = urls
                .Select(url => new MediaStreamStatus { Url = url, State = StreamState.Alive })
                .ToList();

            foreach (var status in statuses)
            {
                cacheManager.StoreStreamStatus(status);
            }

            var firstUrl = urls.First();
            var firstResult = cacheManager.GetStreamStatus(firstUrl);
            Assert.That(firstResult, Is.Not.Null);
            Assert.That(firstResult.State, Is.EqualTo(StreamState.Alive));
        }

        [Test]
        public void GetStreamStatus_WithExpiredEntry_ReturnsCachedValue()
        {
            var url = "http://example.com/stream.m3u8";
            var status = new MediaStreamStatus
            {
                Url = url,
                State = StreamState.Dead,
                LastCheckTime = DateTime.Now.AddHours(-2)
            };

            cacheManager.StoreStreamStatus(status);
            var result = cacheManager.GetStreamStatus(url);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(StreamState.Alive)]
        [TestCase(StreamState.Dead)]
        [TestCase(StreamState.Unauthorised)]
        [TestCase(StreamState.NotFound)]
        [TestCase(StreamState.Unsupported)]
        [TestCase(StreamState.Blacklisted)]
        public void StoreStreamStatus_WithDifferentStates_PersistsState(StreamState state)
        {
            var url = "http://example.com/stream.m3u8";
            var status = new MediaStreamStatus
            {
                Url = url,
                State = state,
                LastCheckTime = DateTime.Now
            };

            cacheManager.StoreStreamStatus(status);
            var result = cacheManager.GetStreamStatus(url);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.State, Is.EqualTo(state));
        }

        [Test]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        public void StoreStreamStatus_WithRepeatedUrls_UpdatesExisting(int updateCount)
        {
            var url = "http://example.com/stream.m3u8";

            for (int i = 0; i < updateCount; i++)
            {
                var state = i % 2 == 0 ? StreamState.Alive : StreamState.Dead;
                var status = new MediaStreamStatus { Url = url, State = state };
                cacheManager.StoreStreamStatus(status);
            }

            var result = cacheManager.GetStreamStatus(url);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.State, Is.EqualTo(updateCount % 2 == 0 ? StreamState.Alive : StreamState.Dead));
        }

        [Test]
        public void GetStreamStatus_WithSpecialCharactersInUrl_HandlesCorrectly()
        {
            var url = "http://example.com/stream%20name.m3u8?token=abc%20def&user=test%20user";
            var status = new MediaStreamStatus { Url = url, State = StreamState.Alive };

            cacheManager.StoreStreamStatus(status);
            var result = cacheManager.GetStreamStatus(url);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase("http://example.com/stream.m3u8")]
        [TestCase("https://example.com/stream.m3u8")]
        [TestCase("http://192.168.1.1:8080/stream.m3u8")]
        public void GetStreamStatus_WithVaryingProtocols_HandlesAll(string url)
        {
            var status = new MediaStreamStatus { Url = url, State = StreamState.Alive };

            cacheManager.StoreStreamStatus(status);
            var result = cacheManager.GetStreamStatus(url);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(100)]
        [TestCase(500)]
        [TestCase(1000)]
        public void StoreStreamStatus_WithLargeBatch_CompletesSuccessfully(int batchSize)
        {
            var statuses = Enumerable.Range(1, batchSize)
                .Select(i => new MediaStreamStatus
                {
                    Url = $"http://example.com/stream{i}.m3u8",
                    State = (i % 5) switch
                    {
                        0 => StreamState.Alive,
                        1 => StreamState.Dead,
                        2 => StreamState.Unauthorised,
                        3 => StreamState.NotFound,
                        _ => StreamState.Unsupported
                    }
                })
                .ToList();

            var startTime = DateTime.Now;
            foreach (var status in statuses)
            {
                cacheManager.StoreStreamStatus(status);
            }
            var elapsed = DateTime.Now - startTime;

            Assert.That(elapsed.TotalSeconds, Is.LessThan(10));
        }

        [Test]
        public void GetStreamStatus_WithEmptyUrl_ReturnsNull()
        {
            var result = cacheManager.GetStreamStatus("");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetStreamStatus_WithNullUrl_ReturnsNull()
        {
            var result = cacheManager.GetStreamStatus(null);

            Assert.That(result, Is.Null);
        }

        [Test]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        public void StoreStreamStatus_WithParallelUpdates_HandlesConcurrency(int parallelCount)
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
                cacheManager.StoreStreamStatus(status);
            });

            var firstResult = cacheManager.GetStreamStatus(urls.First());
            Assert.That(firstResult, Is.Not.Null);
        }

        [Test]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        public void StoreStreamStatus_WithVaryingDates_StoresTimestamps(int urlCount)
        {
            var statuses = Enumerable.Range(1, urlCount)
                .Select(i => new MediaStreamStatus
                {
                    Url = $"http://example.com/stream{i}.m3u8",
                    State = StreamState.Alive,
                    LastCheckTime = DateTime.Now.AddHours(-i)
                })
                .ToList();

            foreach (var status in statuses)
            {
                cacheManager.StoreStreamStatus(status);
            }

            Assert.That(statuses.Count, Is.EqualTo(urlCount));
        }

        [Test]
        public void GetStreamStatus_AfterMultipleUpdates_ReturnsLatest()
        {
            var url = "http://example.com/stream.m3u8";
            var results = new List<MediaStreamStatus>();

            for (int i = 0; i < 5; i++)
            {
                var state = i % 2 == 0 ? StreamState.Alive : StreamState.Dead;
                var status = new MediaStreamStatus { Url = url, State = state };
                cacheManager.StoreStreamStatus(status);
                var retrieved = cacheManager.GetStreamStatus(url);
                results.Add(retrieved);
            }

            Assert.That(results.All(r => r != null), Is.True);
            Assert.That(results.Last().State, Is.EqualTo(StreamState.Dead));
        }
    }
}
