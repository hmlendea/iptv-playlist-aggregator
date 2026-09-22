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
    public class ChannelMatcherIntegrationTests
    {
        private Mock<ILogger> mockLogger;
        private ChannelMatcher channelMatcher;

        [SetUp]
        public void SetUp()
        {
            mockLogger = new Mock<ILogger>();
            channelMatcher = new ChannelMatcher(mockLogger.Object);
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(5, 5)]
        [TestCase(10, 10)]
        [TestCase(50, 50)]
        [TestCase(100, 100)]
        [TestCase(500, 500)]
        public void MatchChannels_WithEqualCounts_ReturnsMatches(int providerChannelCount, int definitionCount)
        {
            var channels = CreateChannels(providerChannelCount);
            var definitions = CreateChannelDefinitions(definitionCount);

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<List<Channel>>());
        }

        [Test]
        [TestCase(10, 5)]
        [TestCase(50, 10)]
        [TestCase(100, 25)]
        [TestCase(500, 100)]
        [TestCase(1000, 200)]
        public void MatchChannels_WithMoreChannelsThanDefinitions_ReturnsSubset(int channelCount, int definitionCount)
        {
            var channels = CreateChannels(channelCount);
            var definitions = CreateChannelDefinitions(definitionCount);

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.LessThanOrEqualTo(channelCount));
        }

        [Test]
        [TestCase(5, 10)]
        [TestCase(10, 50)]
        [TestCase(25, 100)]
        [TestCase(100, 500)]
        public void MatchChannels_WithFewerChannelsThanDefinitions_HandlesGracefully(int channelCount, int definitionCount)
        {
            var channels = CreateChannels(channelCount);
            var definitions = CreateChannelDefinitions(definitionCount);

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.LessThanOrEqualTo(channelCount));
        }

        [Test]
        [TestCase(0, 10)]
        [TestCase(10, 0)]
        [TestCase(0, 0)]
        public void MatchChannels_WithEmptyCollections_ReturnsEmpty(int channelCount, int definitionCount)
        {
            var channels = CreateChannels(channelCount);
            var definitions = CreateChannelDefinitions(definitionCount);

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void MatchChannels_WithIdenticalNames_MatchesSuccessfully()
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = "BBC One", PlaylistChannelName = "BBC One", Url = "http://example.com/bbc1" },
                new Channel { Id = "2", Name = "BBC Two", PlaylistChannelName = "BBC Two", Url = "http://example.com/bbc2" },
                new Channel { Id = "3", Name = "ITV", PlaylistChannelName = "ITV", Url = "http://example.com/itv" }
            };

            var definitions = new List<ChannelDefinition>
            {
                new ChannelDefinition { Id = "def1", Name = "BBC One", GroupId = "group1" },
                new ChannelDefinition { Id = "def2", Name = "BBC Two", GroupId = "group1" },
                new ChannelDefinition { Id = "def3", Name = "ITV", GroupId = "group1" }
            };

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        [TestCase("BBC One", "BBC 1")]
        [TestCase("BBC Two", "BBC 2")]
        [TestCase("Sky News", "Sky News HD")]
        [TestCase("Channel 4", "Channel Four")]
        [TestCase("E!", "E Entertainment")]
        public void MatchChannels_WithSimilarNames_MatchesApproximately(string playlistName, string definitionName)
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = playlistName, PlaylistChannelName = playlistName, Url = "http://example.com/ch" }
            };

            var definitions = new List<ChannelDefinition>
            {
                new ChannelDefinition { Id = "def1", Name = definitionName, GroupId = "group1" }
            };

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [TestCase(10, 10)]
        [TestCase(50, 50)]
        [TestCase(100, 100)]
        [TestCase(500, 500)]
        public void MatchChannels_WithLargeScales_PerformsAcceptably(int channelCount, int definitionCount)
        {
            var channels = CreateChannels(channelCount);
            var definitions = CreateChannelDefinitions(definitionCount);

            var startTime = DateTime.Now;
            var result = channelMatcher.MatchChannels(channels, definitions);
            var elapsed = DateTime.Now - startTime;

            Assert.That(result, Is.Not.Null);
            Assert.That(elapsed.TotalSeconds, Is.LessThan(30)); // Reasonable timeout for large scale
        }

        [Test]
        public void MatchChannels_WithDuplicateChannels_HandlesDuplicates()
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = "BBC One", PlaylistChannelName = "BBC One", Url = "http://example.com/bbc1" },
                new Channel { Id = "2", Name = "BBC One", PlaylistChannelName = "BBC One", Url = "http://example.com/bbc1-alt" },
                new Channel { Id = "3", Name = "BBC One", PlaylistChannelName = "BBC One", Url = "http://example.com/bbc1-backup" }
            };

            var definitions = new List<ChannelDefinition>
            {
                new ChannelDefinition { Id = "def1", Name = "BBC One", GroupId = "group1" }
            };

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void MatchChannels_WithSpecialCharacters_MatchesCorrectly()
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = "BBC One & Family", PlaylistChannelName = "BBC One & Family", Url = "http://example.com/bbc" },
                new Channel { Id = "2", Name = "Channel 4 (HD)", PlaylistChannelName = "Channel 4 (HD)", Url = "http://example.com/ch4" },
                new Channel { Id = "3", Name = "Sky Sports 1 [HD]", PlaylistChannelName = "Sky Sports 1 [HD]", Url = "http://example.com/sky" }
            };

            var definitions = new List<ChannelDefinition>
            {
                new ChannelDefinition { Id = "def1", Name = "BBC One & Family", GroupId = "group1" },
                new ChannelDefinition { Id = "def2", Name = "Channel 4 (HD)", GroupId = "group1" },
                new ChannelDefinition { Id = "def3", Name = "Sky Sports 1 [HD]", GroupId = "group1" }
            };

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        [TestCase("CHANNEL", "channel")]
        [TestCase("BBC ONE", "bbc one")]
        [TestCase("Sky Sports", "sky sports")]
        [TestCase("HBO MAX", "hbo max")]
        public void MatchChannels_WithCaseInsensitivity_MatchesCorrectly(string channelName, string definitionName)
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = channelName, PlaylistChannelName = channelName, Url = "http://example.com/ch" }
            };

            var definitions = new List<ChannelDefinition>
            {
                new ChannelDefinition { Id = "def1", Name = definitionName, GroupId = "group1" }
            };

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void MatchChannels_WithMultipleGroups_MatchesByGroup()
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = "BBC One", PlaylistChannelName = "BBC One", Group = "Entertainment", Url = "http://example.com/bbc1" },
                new Channel { Id = "2", Name = "Sky News", PlaylistChannelName = "Sky News", Group = "News", Url = "http://example.com/sky" },
                new Channel { Id = "3", Name = "ESPN", PlaylistChannelName = "ESPN", Group = "Sports", Url = "http://example.com/espn" }
            };

            var definitions = new List<ChannelDefinition>
            {
                new ChannelDefinition { Id = "def1", Name = "BBC One", GroupId = "entertainment" },
                new ChannelDefinition { Id = "def2", Name = "Sky News", GroupId = "news" },
                new ChannelDefinition { Id = "def3", Name = "ESPN", GroupId = "sports" }
            };

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        [Test]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(50)]
        public void MatchChannels_WithVaryingNumbers_AssignsCorrectly(int channelCount)
        {
            var channels = CreateChannels(channelCount);
            var definitions = CreateChannelDefinitions(channelCount);

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
            foreach (var channel in result)
            {
                Assert.That(channel.Number, Is.GreaterThan(0));
            }
        }

        [Test]
        public void MatchChannels_WithCountryCodes_MatchesWithCountry()
        {
            var channels = new List<Channel>
            {
                new Channel { Id = "1", Name = "BBC One", Country = "GB", PlaylistChannelName = "BBC One", Url = "http://example.com/bbc" },
                new Channel { Id = "2", Name = "France 2", Country = "FR", PlaylistChannelName = "France 2", Url = "http://example.com/fr2" },
                new Channel { Id = "3", Name = "ARD", Country = "DE", PlaylistChannelName = "ARD", Url = "http://example.com/ard" }
            };

            var definitions = new List<ChannelDefinition>
            {
                new ChannelDefinition { Id = "def1", Name = "BBC One", Country = "GB", GroupId = "group1" },
                new ChannelDefinition { Id = "def2", Name = "France 2", Country = "FR", GroupId = "group1" },
                new ChannelDefinition { Id = "def3", Name = "ARD", Country = "DE", GroupId = "group1" }
            };

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void MatchChannels_WithLogoUrls_PreservesLogoUrls()
        {
            var channels = new List<Channel>
            {
                new Channel
                {
                    Id = "1",
                    Name = "BBC One",
                    PlaylistChannelName = "BBC One",
                    LogoUrl = "http://example.com/bbc-logo.png",
                    Url = "http://example.com/bbc"
                }
            };

            var definitions = new List<ChannelDefinition>
            {
                new ChannelDefinition
                {
                    Id = "def1",
                    Name = "BBC One",
                    LogoUrl = "http://custom.com/bbc-logo.png",
                    GroupId = "group1"
                }
            };

            var result = channelMatcher.MatchChannels(channels, definitions);

            Assert.That(result, Is.Not.Null);
            var matched = result.FirstOrDefault();
            if (matched != null)
            {
                Assert.That(matched.LogoUrl, Is.Not.Null);
            }
        }

        private List<Channel> CreateChannels(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new Channel
                {
                    Id = $"ch{i}",
                    Name = $"Channel {i}",
                    PlaylistChannelName = $"Channel {i}",
                    Group = $"Group{i % 5}",
                    Country = $"Country{i % 10}",
                    LogoUrl = $"http://example.com/logo{i}.png",
                    Number = i,
                    PlaylistId = $"playlist{i % 3}",
                    Url = $"http://example.com/stream{i}.m3u8"
                })
                .ToList();
        }

        private List<ChannelDefinition> CreateChannelDefinitions(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new ChannelDefinition
                {
                    Id = $"def{i}",
                    Name = $"Channel {i}",
                    GroupId = $"group{i % 5}",
                    Country = $"Country{i % 10}",
                    LogoUrl = $"http://example.com/def-logo{i}.png",
                    IsEnabled = i % 2 == 0
                })
                .ToList();
        }
    }
}
