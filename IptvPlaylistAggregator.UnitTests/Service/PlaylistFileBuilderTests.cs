using System;

using Moq;
using NUnit.Framework;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.UnitTests.Service
{
    public sealed class PlaylistFileBuilderTests
    {
        private Mock<ICacheManager> cacheMock;
        private ApplicationSettings applicationSettings;

        private PlaylistFileBuilder playlistFileBuilder;

        [SetUp]
        public void SetUp()
        {
            cacheMock = new Mock<ICacheManager>();
            applicationSettings = new ApplicationSettings();

            playlistFileBuilder = new PlaylistFileBuilder(cacheMock.Object, applicationSettings);
        }

        [Test]
        public void TryParseFile_GivenNullContent_ThenNullIsReturned()
        {
            Playlist result = playlistFileBuilder.TryParseFile(null);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryParseFile_GivenEmptyContent_ThenNullIsReturned()
        {
            Playlist result = playlistFileBuilder.TryParseFile(string.Empty);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryParseFile_GivenWhitespaceContent_ThenNullIsReturned()
        {
            Playlist result = playlistFileBuilder.TryParseFile("   ");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryParseFile_GivenUrlLineBeforeExtinfHeader_ThenNullIsReturned()
        {
            string content = "http://test.nucilandia.ro/stream1\n#EXTINF:-1,Sport TV\nhttp://test.nucilandia.ro/stream2";

            Playlist result = playlistFileBuilder.TryParseFile(content);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryParseFile_GivenValidContent_ThenPlaylistIsReturned()
        {
            string content = "#EXTM3U\n#EXTINF:-1,Sport TV\nhttp://test.nucilandia.ro/stream1";

            Playlist result = playlistFileBuilder.TryParseFile(content);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ParseFile_GivenNullContent_ThenArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => playlistFileBuilder.ParseFile(null));
        }

        [Test]
        public void ParseFile_GivenEmptyContent_ThenArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => playlistFileBuilder.ParseFile(string.Empty));
        }

        [Test]
        public void ParseFile_GivenWhitespaceContent_ThenArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => playlistFileBuilder.ParseFile("   "));
        }

        [TestCase("#EXTM3U\n#EXTINF:-1,Sport TV\nhttp://test.nucilandia.ro/stream1\n", "Sport TV", "http://test.nucilandia.ro/stream1")]
        [TestCase("#EXTM3U\r\n#EXTINF:-1,Sport TV\r\nhttp://test.nucilandia.ro/stream1\r\n", "Sport TV", "http://test.nucilandia.ro/stream1")]
        [Test]
        public void ParseFile_GivenValidSingleChannelContent_ThenChannelIsParsedCorrectly(
            string content,
            string expectedChannelName,
            string expectedUrl)
        {
            Playlist result = playlistFileBuilder.ParseFile(content);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Channels, Has.Count.EqualTo(1));
            Assert.That(result.Channels[0].Name, Is.EqualTo(expectedChannelName));
            Assert.That(result.Channels[0].Url, Is.EqualTo(expectedUrl));
        }

        [Test]
        public void ParseFile_GivenValidSingleChannelContent_ThenPlaylistChannelNameMatchesChannelName()
        {
            string content = "#EXTM3U\n#EXTINF:-1,Sport TV\nhttp://test.nucilandia.ro/stream1";

            Playlist result = playlistFileBuilder.ParseFile(content);

            Assert.That(result.Channels[0].PlaylistChannelName, Is.EqualTo(result.Channels[0].Name));
        }

        [Test]
        public void ParseFile_GivenMultipleChannels_ThenAllChannelsAreParsed()
        {
            string content = string.Join("\n",
                "#EXTM3U",
                "#EXTINF:-1,Sport TV",
                "http://test.nucilandia.ro/stream1",
                "#EXTINF:-1,News Channel",
                "http://test.nucilandia.ro/stream2");

            Playlist result = playlistFileBuilder.ParseFile(content);

            Assert.That(result.Channels, Has.Count.EqualTo(2));
            Assert.That(result.Channels[0].Name, Is.EqualTo("Sport TV"));
            Assert.That(result.Channels[0].Url, Is.EqualTo("http://test.nucilandia.ro/stream1"));
            Assert.That(result.Channels[1].Name, Is.EqualTo("News Channel"));
            Assert.That(result.Channels[1].Url, Is.EqualTo("http://test.nucilandia.ro/stream2"));
        }

        [Test]
        public void ParseFile_GivenCommentLines_ThenCommentLinesAreIgnored()
        {
            string content = string.Join("\n",
                "#EXTM3U",
                "# This is a comment line",
                "#EXTINF:-1,Sport TV",
                "http://test.nucilandia.ro/stream1");

            Playlist result = playlistFileBuilder.ParseFile(content);

            Assert.That(result.Channels, Has.Count.EqualTo(1));
        }

        [Test]
        public void ParseFile_GivenExtXStreamInfLine_ThenChannelIsAddedWithNullNameAndCorrectUrl()
        {
            string content = string.Join("\n",
                "#EXTM3U",
                "#EXT-X-STREAM-INF:BANDWIDTH=613000",
                "http://test.nucilandia.ro/stream1");

            Playlist result = playlistFileBuilder.ParseFile(content);

            Assert.That(result.Channels, Has.Count.EqualTo(1));
            Assert.That(result.Channels[0].Name, Is.Null);
            Assert.That(result.Channels[0].Url, Is.EqualTo("http://test.nucilandia.ro/stream1"));
        }

        [Test]
        public void ParseFile_GivenCachedPlaylist_ThenCachedPlaylistIsReturned()
        {
            string content = "#EXTM3U\n#EXTINF:-1,Sport TV\nhttp://test.nucilandia.ro/stream1";
            Playlist cachedPlaylist = new();

            cacheMock.Setup(cache => cache.GetPlaylist(content)).Returns(cachedPlaylist);

            Playlist result = playlistFileBuilder.ParseFile(content);

            Assert.That(result, Is.SameAs(cachedPlaylist));
        }

        [Test]
        public void ParseFile_GivenCachedPlaylist_ThenPlaylistIsNotReprocessed()
        {
            string content = "#EXTM3U\n#EXTINF:-1,Sport TV\nhttp://test.nucilandia.ro/stream1";
            Playlist cachedPlaylist = new();

            cacheMock.Setup(cache => cache.GetPlaylist(content)).Returns(cachedPlaylist);

            playlistFileBuilder.ParseFile(content);

            cacheMock.Verify(
                cache => cache.StorePlaylist(It.IsAny<string>(), It.IsAny<Playlist>()),
                Times.Never);
        }

        [Test]
        public void ParseFile_WhenNotCached_ThenResultIsStoredInCache()
        {
            string content = "#EXTM3U\n#EXTINF:-1,Sport TV\nhttp://test.nucilandia.ro/stream1";

            playlistFileBuilder.ParseFile(content);

            cacheMock.Verify(
                cache => cache.StorePlaylist(content, It.IsAny<Playlist>()),
                Times.Once);
        }

        [Test]
        public void BuildFile_GivenEmptyPlaylist_ThenOnlyFileHeaderIsReturned()
        {
            Playlist playlist = new();

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Is.EqualTo("#EXTM3U" + Environment.NewLine));
        }

        [Test]
        public void BuildFile_GivenSingleChannelWithNoTagsEnabled_ThenChannelIsInCorrectFormat()
        {
            applicationSettings.AreTvGuideTagsEnabled = false;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel { Name = "Sport TV", Url = "http://test.nucilandia.ro/stream1" });

            string result = playlistFileBuilder.BuildFile(playlist);

            string expected =
                "#EXTM3U" + Environment.NewLine +
                "#EXTINF:-1,Sport TV" + Environment.NewLine +
                "http://test.nucilandia.ro/stream1" + Environment.NewLine;

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildFile_GivenMultipleChannels_ThenAllChannelsArePresent()
        {
            applicationSettings.AreTvGuideTagsEnabled = false;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel { Name = "Sport TV", Url = "http://test.nucilandia.ro/stream1" });
            playlist.Channels.Add(new Channel { Name = "News Channel", Url = "http://test.nucilandia.ro/stream2" });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Contain("Sport TV"));
            Assert.That(result, Does.Contain("http://test.nucilandia.ro/stream1"));
            Assert.That(result, Does.Contain("News Channel"));
            Assert.That(result, Does.Contain("http://test.nucilandia.ro/stream2"));
        }

        [Test]
        public void BuildFile_GivenTvGuideTagsEnabled_ThenChannelNumberAndNameTagsAreIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = true;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel { Name = "Sport TV", Url = "http://test.nucilandia.ro/stream1", Number = 613 });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Contain("tvg-chno=\"613\""));
            Assert.That(result, Does.Contain("tvg-name=\"Sport TV\""));
        }

        [Test]
        public void BuildFile_GivenTvGuideTagsEnabledAndChannelHasId_ThenTvGuideIdTagIsIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = true;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel
            {
                Name = "Sport TV",
                Url = "http://test.nucilandia.ro/stream1",
                Id = "sporttv.ro"
            });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Contain("tvg-id=\"sporttv.ro\""));
        }

        [Test]
        public void BuildFile_GivenTvGuideTagsEnabledAndChannelHasLogoUrl_ThenTvGuideLogoTagIsIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = true;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel
            {
                Name = "Sport TV",
                Url = "http://test.nucilandia.ro/stream1",
                LogoUrl = "http://logos.nucilandia.ro/sporttv.png"
            });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Contain("tvg-logo=\"http://logos.nucilandia.ro/sporttv.png\""));
        }

        [Test]
        public void BuildFile_GivenTvGuideTagsEnabledAndChannelHasNoLogoUrl_ThenTvGuideLogoTagIsNotIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = true;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel { Name = "Sport TV", Url = "http://test.nucilandia.ro/stream1", LogoUrl = null });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Not.Contain("tvg-logo="));
        }

        [Test]
        public void BuildFile_GivenTvGuideTagsEnabledAndChannelHasCountry_ThenTvGuideCountryTagIsIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = true;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel { Name = "Sport TV", Url = "http://test.nucilandia.ro/stream1", Country = "RO" });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Contain("tvg-country=\"RO\""));
        }

        [Test]
        public void BuildFile_GivenTvGuideTagsEnabledAndChannelHasNoCountry_ThenTvGuideCountryTagIsNotIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = true;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel { Name = "Sport TV", Url = "http://test.nucilandia.ro/stream1", Country = null });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Not.Contain("tvg-country="));
        }

        [Test]
        public void BuildFile_GivenTvGuideTagsEnabledAndChannelHasGroup_ThenTvGuideGroupTagIsIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = true;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel { Name = "Sport TV", Url = "http://test.nucilandia.ro/stream1", Group = "Sports" });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Contain("group-title=\"Sports\""));
        }

        [Test]
        public void BuildFile_GivenTvGuideTagsEnabledAndChannelHasNoGroup_ThenTvGuideGroupTagIsNotIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = true;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel { Name = "Sport TV", Url = "http://test.nucilandia.ro/stream1", Group = null });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Not.Contain("group-title="));
        }

        [Test]
        public void BuildFile_GivenPlaylistDetailsTagsEnabled_ThenPlaylistIdAndChannelNameTagsAreIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = false;
            applicationSettings.ArePlaylistDetailsTagsEnabled = true;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel
            {
                Name = "Sport TV",
                Url = "http://test.nucilandia.ro/stream1",
                PlaylistId = "playlist-873",
                PlaylistChannelName = "RO: Sport TV"
            });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Contain("playlist-id=\"playlist-873\""));
            Assert.That(result, Does.Contain("playlist-channel-name=\"RO: Sport TV\""));
        }

        [Test]
        public void BuildFile_GivenTvGuideTagsDisabled_ThenTvGuideTagsAreNotIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = false;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel { Name = "Sport TV", Url = "http://test.nucilandia.ro/stream1", Number = 613 });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Not.Contain("tvg-chno="));
            Assert.That(result, Does.Not.Contain("tvg-name="));
        }

        [Test]
        public void BuildFile_GivenPlaylistDetailsTagsDisabled_ThenPlaylistTagsAreNotIncluded()
        {
            applicationSettings.AreTvGuideTagsEnabled = false;
            applicationSettings.ArePlaylistDetailsTagsEnabled = false;

            Playlist playlist = new();
            playlist.Channels.Add(new Channel
            {
                Name = "Sport TV",
                Url = "http://test.nucilandia.ro/stream1",
                PlaylistId = "playlist-873"
            });

            string result = playlistFileBuilder.BuildFile(playlist);

            Assert.That(result, Does.Not.Contain("playlist-id="));
            Assert.That(result, Does.Not.Contain("playlist-channel-name="));
        }
    }
}
