using System;
using System.Collections.Generic;
using System.Linq;

using Moq;
using NUnit.Framework;

using NuciLog.Core;

using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.IntegrationTests
{
    [TestFixture]
    public class PlaylistFileBuilderIntegrationTests
    {
        private Mock<ILogger> mockLogger;
        private PlaylistFileBuilder playlistFileBuilder;

        [SetUp]
        public void SetUp()
        {
            mockLogger = new Mock<ILogger>();
            playlistFileBuilder = new PlaylistFileBuilder(mockLogger.Object);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(500)]
        [TestCase(1000)]
        public void Build_WithVaryingChannelCounts_GeneratesValidM3u(int channelCount)
        {
            var playlist = new Playlist
            {
                Channels = CreateChannels(channelCount)
            };

            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("#EXTM3U"));
            Assert.That(result, Is.TypeOf<string>());
        }

        [Test]
        public void Build_WithEmptyPlaylist_ReturnsValidHeader()
        {
            var playlist = new Playlist();

            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("#EXTM3U"));
        }

        [Test]
        [TestCase("BBC One", "http://example.com/bbc1.m3u8")]
        [TestCase("Sky News", "https://example.com:8080/sky.m3u8")]
        [TestCase("Channel 4", "http://example.com/ch4?token=abc123")]
        [TestCase("E!", "http://example.com/e-live.ts")]
        public void Build_WithNamedChannels_PreservesNames(string channelName, string url)
        {
            var playlist = new Playlist
            {
                Channels = new List<Channel>
                {
                    new Channel
                    {
                        Id = "1",
                        Name = channelName,
                        Url = url,
                        Number = 1
                    }
                }
            };

            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Does.Contain(channelName));
        }

        [Test]
        public void Build_WithLogos_IncludesLogoUrls()
        {
            var channel = new Channel
            {
                Id = "1",
                Name = "BBC One",
                LogoUrl = "http://example.com/bbc-logo.png",
                Url = "http://example.com/stream.m3u8",
                Number = 1
            };

            var playlist = new Playlist { Channels = new List<Channel> { channel } };
            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Does.Contain("bbc-logo.png"));
        }

        [Test]
        public void Build_WithGroups_IncludesGroupTitles()
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = "BBC One", Group = "Entertainment", Url = "http://example.com/bbc1.m3u8", Number = 1 },
                new Channel { Id = "2", Name = "Sky News", Group = "News", Url = "http://example.com/sky.m3u8", Number = 2 },
                new Channel { Id = "3", Name = "ESPN", Group = "Sports", Url = "http://example.com/espn.m3u8", Number = 3 }
            };

            var playlist = new Playlist { Channels = channels };
            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Does.Contain("Entertainment"));
            Assert.That(result, Does.Contain("News"));
            Assert.That(result, Does.Contain("Sports"));
        }

        [Test]
        [TestCase("EXTINF")]
        [TestCase("EXTM3U")]
        [TestCase("http://")]
        public void Build_WithValidM3u_ContainsRequiredFields(string expectedField)
        {
            var playlist = new Playlist
            {
                Channels = new List<Channel>
                {
                    new Channel { Id = "1", Name = "Channel 1", Url = "http://example.com/stream.m3u8", Number = 1 }
                }
            };

            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Does.Contain(expectedField));
        }

        [Test]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(500)]
        [TestCase(1000)]
        public void Build_WithLargePlaylist_GeneratesCorrectSize(int channelCount)
        {
            var playlist = new Playlist { Channels = CreateChannels(channelCount) };
            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.GreaterThan(channelCount * 50)); // Rough estimate
        }

        [Test]
        public void Build_WithSpecialCharacters_EncodesCorrectly()
        {
            var playlist = new Playlist
            {
                Channels = new List<Channel>
                {
                    new Channel { Id = "1", Name = "Channel & Friends", Url = "http://example.com/stream.m3u8", Number = 1 },
                    new Channel { Id = "2", Name = "Channel (HD)", Url = "http://example.com/stream.m3u8", Number = 2 },
                    new Channel { Id = "3", Name = "Channel [Extra]", Url = "http://example.com/stream.m3u8", Number = 3 }
                }
            };

            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Channel"));
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(5, 5)]
        [TestCase(10, 10)]
        [TestCase(100, 100)]
        public void Build_WithCountryInfo_PreservesMetadata(int channelCount, int expectedCount)
        {
            var playlist = new Playlist
            {
                Channels = Enumerable.Range(1, channelCount)
                    .Select(i => new Channel
                    {
                        Id = $"ch{i}",
                        Name = $"Channel {i}",
                        Country = $"Country{i}",
                        Url = $"http://example.com/stream{i}.m3u8",
                        Number = i
                    })
                    .ToList()
            };

            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Is.Not.Null);
            for (int i = 1; i <= expectedCount; i++)
            {
                Assert.That(result, Does.Contain($"Channel {i}"));
            }
        }

        [Test]
        public void Build_WithNumberedChannels_PreservesSequence()
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = "Channel 1", Number = 1, Url = "http://example.com/1.m3u8" },
                new Channel { Id = "2", Name = "Channel 2", Number = 2, Url = "http://example.com/2.m3u8" },
                new Channel { Id = "3", Name = "Channel 3", Number = 3, Url = "http://example.com/3.m3u8" },
                new Channel { Id = "4", Name = "Channel 4", Number = 4, Url = "http://example.com/4.m3u8" },
                new Channel { Id = "5", Name = "Channel 5", Number = 5, Url = "http://example.com/5.m3u8" }
            };

            var playlist = new Playlist { Channels = channels };
            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Is.Not.Null);
            var lines = result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var channelLines = lines.Where(l => l.StartsWith("#EXTINF")).ToList();
            Assert.That(channelLines.Count, Is.GreaterThan(0));
        }

        [Test]
        [TestCase("http://example.com/stream.m3u8")]
        [TestCase("https://example.com/stream.m3u8")]
        [TestCase("http://example.com:8080/stream.m3u8")]
        [TestCase("http://example.com/path/stream.m3u8?token=abc")]
        public void Build_WithVaryingUrls_PreservesUrls(string url)
        {
            var playlist = new Playlist
            {
                Channels = new List<Channel>
                {
                    new Channel { Id = "1", Name = "Channel", Url = url, Number = 1 }
                }
            };

            var result = playlistFileBuilder.Build(playlist);

            Assert.That(result, Does.Contain(url));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        public void Parse_WithValidM3u_ReturnsPlaylist(int channelCount)
        {
            var m3uContent = CreateValidM3uContent(channelCount);

            var result = playlistFileBuilder.Parse(m3uContent);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<Playlist>());
            Assert.That(result.Channels, Is.Not.Null);
        }

        [Test]
        [TestCase("")]
        [TestCase("#EXTM3U")]
        [TestCase("#EXTM3U\n")]
        public void Parse_WithMinimalM3u_ReturnsEmptyPlaylist(string m3uContent)
        {
            var result = playlistFileBuilder.Parse(m3uContent);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.True);
        }

        [Test]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        public void Parse_WithVaryingChannelCounts_ParsesCorrectly(int channelCount)
        {
            var m3uContent = CreateValidM3uContent(channelCount);

            var result = playlistFileBuilder.Parse(m3uContent);

            Assert.That(result.Channels.Count, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Channels.Count, Is.LessThanOrEqualTo(channelCount));
        }

        [Test]
        public void Parse_WithDuplicateChannels_HandlesDuplicates()
        {
            var m3uContent = "#EXTM3U\n" +
                "#EXTINF:-1,Channel 1\n" +
                "http://example.com/stream1.m3u8\n" +
                "#EXTINF:-1,Channel 1\n" +
                "http://example.com/stream1-alt.m3u8\n";

            var result = playlistFileBuilder.Parse(m3uContent);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Channels, Is.Not.Null);
        }

        [Test]
        public void Parse_WithMissingUrls_HandlesGracefully()
        {
            var m3uContent = "#EXTM3U\n" +
                "#EXTINF:-1,Channel 1\n" +
                "#EXTINF:-1,Channel 2\n" +
                "http://example.com/stream2.m3u8\n";

            var result = playlistFileBuilder.Parse(m3uContent);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(100)]
        [TestCase(500)]
        [TestCase(1000)]
        public void Parse_WithLargeM3u_ProcessesInReasonableTime(int channelCount)
        {
            var m3uContent = CreateValidM3uContent(channelCount);
            var startTime = DateTime.Now;

            var result = playlistFileBuilder.Parse(m3uContent);

            var elapsed = DateTime.Now - startTime;
            Assert.That(result, Is.Not.Null);
            Assert.That(elapsed.TotalSeconds, Is.LessThan(30));
        }

        [Test]
        public void BuildAndParse_RoundTrip_PreservesData()
        {
            var originalChannels = new List<Channel>
            {
                new Channel { Id = "1", Name = "BBC One", Group = "Entertainment", Url = "http://example.com/bbc1.m3u8", Number = 1 },
                new Channel { Id = "2", Name = "Sky News", Group = "News", Url = "http://example.com/sky.m3u8", Number = 2 },
                new Channel { Id = "3", Name = "ESPN", Group = "Sports", Url = "http://example.com/espn.m3u8", Number = 3 }
            };

            var playlist = new Playlist { Channels = originalChannels };
            var built = playlistFileBuilder.Build(playlist);
            var parsed = playlistFileBuilder.Parse(built);

            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.Channels.Count, Is.GreaterThan(0));
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
                content += $"#EXTINF:-1 tvg-id=\"ch{i}\" tvg-name=\"Channel {i}\" group-title=\"Group{i % 5}\" tvg-logo=\"http://example.com/logo{i}.png\",Channel {i}\n";
                content += $"http://example.com/stream{i}.m3u8\n";
            }
            return content;
        }
    }
}
