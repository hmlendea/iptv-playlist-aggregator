using NUnit.Framework;

using Moq;

using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.UnitTests.Service.Models
{
    public sealed class ChannelMatcherTests
    {
        Mock<ICacheManager> cacheMock;

        IChannelMatcher channelMatcher;

        [SetUp]
        public void SetUp()
        {
            cacheMock = new Mock<ICacheManager>();

            channelMatcher = new ChannelMatcher(cacheMock.Object);
        }

        [TestCase("Ardeal TV", "RO: Ardeal TV", "|RO| Ardeal TV")]
        [TestCase("Cartoon Network", "RO: Cartoon Network", "VIP|RO|: Cartoon Network")]
        [TestCase("Digi Sport 2", "RO: Digi Sport 2", "RO: DIGI Sport 2")]
        [TestCase("România TV", "România TV", "RO\" Romania TV")]
        [TestCase("Somax TV", null, "Somax TV")]
        [Test]
        public void DoesMatch_NamesMatch_ReturnsTrue(
            string definedName,
            string alias,
            string providerName)
        {
            ChannelName channelName;
            
            if (alias is null)
            {
                channelName = new ChannelName(definedName);
            }
            else
            {
                channelName = new ChannelName(definedName, alias);
            }

            Assert.IsTrue(channelMatcher.DoesMatch(channelName, providerName));
        }

        [Test]
        public void DoesMatch_CompareWithDifferentValue_ReturnsFalse()
        {
            string definedName = "Telekom Sport 2";
            string providerName = "RO: Digi Sport 2";
            string alias = "RO: Telekom Sport 2";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsFalse(channelMatcher.DoesMatch(channelName, providerName));
        }

        [TestCase("VIP|RO|: Discovery Channel FHD", "DISCOVERYCHANNEL")]
        [Test]
        public void NormaliseName_ReturnsExpectedValue(string inputValue, string expectedValue)
        {
            string actualValue = channelMatcher.NormaliseName(inputValue);
            
            Assert.AreEqual(expectedValue, actualValue);
        }
    }
}
