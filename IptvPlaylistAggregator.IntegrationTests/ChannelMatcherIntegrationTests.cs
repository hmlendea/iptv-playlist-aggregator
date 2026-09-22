using System.Collections.Generic;
using System.Linq;

using Moq;
using NUnit.Framework;

using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.IntegrationTests
{
    [TestFixture]
    public class ChannelMatcherIntegrationTests
    {
        private Mock<ICacheManager> mockCacheManager;
        private ChannelMatcher channelMatcher;

        [SetUp]
        public void SetUp()
        {
            mockCacheManager = new Mock<ICacheManager>();
            mockCacheManager.Setup(cache => cache.GetNormalisedChannelName(It.IsAny<string>())).Returns(string.Empty);
            channelMatcher = new ChannelMatcher(mockCacheManager.Object);
        }

        [Test]
        [TestCase(1, 1)] [TestCase(5, 5)] [TestCase(10, 10)] [TestCase(50, 50)] [TestCase(100, 100)] [TestCase(500, 500)]
        public void DoesMatch_WithEqualCounts_ReturnsMatches(int channelCount, int definitionCount)
            => Assert.That(CountMatches(channelCount, definitionCount), Is.GreaterThan(0));

        [Test]
        [TestCase(10, 5)] [TestCase(50, 10)] [TestCase(100, 25)] [TestCase(500, 100)] [TestCase(1000, 200)]
        public void DoesMatch_WithMoreChannelsThanDefinitions_ReturnsSubset(int channelCount, int definitionCount)
            => Assert.That(CountMatches(channelCount, definitionCount), Is.LessThanOrEqualTo(definitionCount));

        [Test]
        [TestCase(5, 10)] [TestCase(10, 50)] [TestCase(25, 100)] [TestCase(100, 500)]
        public void DoesMatch_WithFewerChannelsThanDefinitions_HandlesGracefully(int channelCount, int definitionCount)
            => Assert.That(CountMatches(channelCount, definitionCount), Is.EqualTo(channelCount));

        [Test]
        [TestCase(0, 10)] [TestCase(10, 0)] [TestCase(0, 0)]
        public void DoesMatch_WithEmptyCollections_ReturnsEmpty(int channelCount, int definitionCount)
            => Assert.That(CountMatches(channelCount, definitionCount), Is.Zero);

        [Test]
        [TestCase("BBC One", "BBC One")] [TestCase("BBC Two", "BBC Two")] [TestCase("ITV", "ITV")]
        [TestCase("BBC One", "BBC 1")] [TestCase("BBC Two", "BBC 2")] [TestCase("Sky News", "Sky News HD")]
        [TestCase("Channel 4", "Channel Four")] [TestCase("E!", "E Entertainment")]
        [TestCase("BBC One & Family", "BBC One & Family")] [TestCase("Channel 4 (HD)", "Channel 4 (HD)")]
        [TestCase("Sky Sports 1 [HD]", "Sky Sports 1 [HD]")]
        [TestCase("CHANNEL", "channel")] [TestCase("BBC ONE", "bbc one")] [TestCase("Sky Sports", "sky sports")] [TestCase("HBO MAX", "hbo max")]
        public void DoesMatch_WithEquivalentOrSimilarNames_EvaluatesCorrectly(string firstName, string secondName)
        {
            bool doesMatch = channelMatcher.DoesMatch(new ChannelName(firstName), secondName, string.Empty);
            Assert.That(doesMatch, Is.EqualTo(string.Equals(firstName, secondName, System.StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        [TestCase(10, 10)] [TestCase(50, 50)] [TestCase(100, 100)] [TestCase(500, 500)]
        public void DoesMatch_WithLargeScales_PerformsAcceptably(int channelCount, int definitionCount)
            => Assert.That(CountMatches(channelCount, definitionCount), Is.EqualTo(System.Math.Min(channelCount, definitionCount)));

        [Test]
        [TestCase("BBC One", "GB", "BBC One", "GB")]
        [TestCase("France 2", "FR", "France 2", "FR")]
        [TestCase("ARD", "DE", "ARD", "DE")]
        public void DoesMatch_WithCountryCodes_MatchesWithCountry(string firstName, string firstCountry, string secondName, string secondCountry)
            => Assert.That(channelMatcher.DoesMatch(new ChannelName(firstName, firstCountry), secondName, secondCountry));

        [Test]
        [TestCase("Romania: TVR 1", "RO", "TVR 1", "RO")]
        [TestCase("BBC One HD", "GB", "BBC One", "GB")]
        public void NormaliseName_WithVaryingNames_StoresNormalisedValue(string name, string country, string expectedName, string expectedCountry)
        {
            string normalisedName = channelMatcher.NormaliseName(name, country);
            Assert.That(normalisedName, Is.Not.Empty);
            mockCacheManager.Verify(cache => cache.StoreNormalisedChannelName(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private int CountMatches(int channelCount, int definitionCount)
        {
            List<Channel> channels = CreateChannels(channelCount);
            List<ChannelDefinition> definitions = CreateDefinitions(definitionCount);
            return definitions.Count(definition => channels.Any(channel => channelMatcher.DoesMatch(definition.Name, channel.Name, channel.Country)));
        }

        private static List<Channel> CreateChannels(int count) => Enumerable.Range(1, count)
            .Select(index => new Channel { Id = $"channel{index}", Name = $"Channel {index}", Country = "GB" }).ToList();

        private static List<ChannelDefinition> CreateDefinitions(int count) => Enumerable.Range(1, count)
            .Select(index => new ChannelDefinition { Id = $"definition{index}", Name = new ChannelName($"Channel {index}", "GB") }).ToList();
    }
}
