using System;
using System.Collections.Generic;
using System.Linq;

using Moq;
using NUnit.Framework;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.IntegrationTests
{
    [TestFixture]
    public class PlaylistFileBuilderIntegrationTests
    {
        private Mock<ICacheManager> mockCacheManager;
        private PlaylistFileBuilder playlistFileBuilder;

        [SetUp]
        public void SetUp()
        {
            mockCacheManager = new Mock<ICacheManager>();
            playlistFileBuilder = new PlaylistFileBuilder(mockCacheManager.Object, new ApplicationSettings { AreTvGuideTagsEnabled = true, ArePlaylistDetailsTagsEnabled = true });
        }

        [Test]
        [TestCase(0)] [TestCase(1)] [TestCase(5)] [TestCase(10)] [TestCase(50)] [TestCase(100)] [TestCase(500)] [TestCase(1000)]
        public void BuildFile_WithVaryingChannelCounts_GeneratesValidM3u(int channelCount)
        {
            string file = playlistFileBuilder.BuildFile(CreatePlaylist(channelCount));
            Assert.That(file, Does.StartWith("#EXTM3U"));
        }

        [Test]
        [TestCase("BBC One", "http://example.com/bbc1.m3u8")] [TestCase("Sky News", "https://example.com:8080/sky.m3u8")]
        [TestCase("Channel 4", "http://example.com/ch4?token=abc123")] [TestCase("E!", "http://example.com/e-live.ts")]
        public void BuildFile_WithNamedChannels_PreservesNames(string name, string url)
        {
            Playlist playlist = CreatePlaylist(0);
            playlist.Channels.Add(new Channel { Id = "channel", Name = name, Url = url, Number = 1 });
            Assert.That(playlistFileBuilder.BuildFile(playlist), Does.Contain(name));
        }

        [Test]
        [TestCase("EXTINF")] [TestCase("EXTM3U")] [TestCase("http://")]
        public void BuildFile_WithValidM3u_ContainsRequiredFields(string expectedField)
            => Assert.That(playlistFileBuilder.BuildFile(CreatePlaylist(1)), Does.Contain(expectedField));

        [Test]
        [TestCase(1)] [TestCase(5)] [TestCase(10)] [TestCase(50)] [TestCase(100)] [TestCase(500)] [TestCase(1000)]
        public void BuildFile_WithLargePlaylist_GeneratesContent(int channelCount)
            => Assert.That(playlistFileBuilder.BuildFile(CreatePlaylist(channelCount)), Is.Not.Empty);

        [Test]
        [TestCase("http://example.com/stream.m3u8")] [TestCase("https://example.com/stream.m3u8")]
        [TestCase("http://example.com:8080/stream.m3u8")] [TestCase("http://example.com/path/stream.m3u8?token=abc")]
        public void BuildFile_WithVaryingUrls_PreservesUrls(string url)
        {
            Playlist playlist = CreatePlaylist(0);
            playlist.Channels.Add(new Channel { Id = "channel", Name = "Channel", Url = url, Number = 1 });
            Assert.That(playlistFileBuilder.BuildFile(playlist), Does.Contain(url));
        }

        [Test]
        [TestCase(0)] [TestCase(1)] [TestCase(10)] [TestCase(50)] [TestCase(100)] [TestCase(500)] [TestCase(1000)]
        public void ParseFile_WithValidM3u_ReturnsPlaylist(int channelCount)
        {
            Playlist playlist = playlistFileBuilder.ParseFile(CreateM3u(channelCount));
            Assert.That(playlist.Channels, Has.Count.EqualTo(channelCount));
        }

        [Test]
        [TestCase("")] [TestCase("#EXTM3U")] [TestCase("#EXTM3U\n")]
        public void TryParseFile_WithMinimalM3u_ReturnsExpectedResult(string file)
        {
            Playlist playlist = playlistFileBuilder.TryParseFile(file);
            Assert.That(playlist is null || playlist.IsEmpty);
        }

        [Test]
        public void BuildFileAndParseFile_RoundTrip_PreservesChannels()
        {
            Playlist parsed = playlistFileBuilder.ParseFile(playlistFileBuilder.BuildFile(CreatePlaylist(3)));
            Assert.That(parsed.Channels, Has.Count.EqualTo(3));
        }

        private static Playlist CreatePlaylist(int count)
        {
            Playlist playlist = new();
            playlist.Channels.AddRange(Enumerable.Range(1, count).Select(index => new Channel { Id = $"channel{index}", Name = $"Channel {index}", Url = $"http://example.com/{index}.m3u8", Number = index }));
            return playlist;
        }

        private static string CreateM3u(int count) => "#EXTM3U\n" + string.Concat(Enumerable.Range(1, count).Select(index => $"#EXTINF:-1,Channel {index}\nhttp://example.com/{index}.m3u8\n"));
    }
}
