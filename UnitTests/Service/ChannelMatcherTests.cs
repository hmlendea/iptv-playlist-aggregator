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
        [TestCase("Digi World", "RO: Digi World FHD", "RUMANIA: DigiWorld FHD (Opt-1)")]
        [TestCase("Realitatea Plus", null, "Realitatea Plus")]
        [TestCase("România TV", "România TV", "RO\" Romania TV")]
        [TestCase("Somax", "RO: Somax TV", "Somax TV")]
        [TestCase("TVR Târgu Mureș", "RO: TVR T?rgu-Mure?", "TVR: Targu Mureș")]
        [TestCase("U TV", null, "UTV")]
        [Test]
        public void DoesMatch_NamesMatch_ReturnsTrue(
            string definedName,
            string alias,
            string providerName)
        {
            ChannelName channelName = GetChannelName(definedName, alias);

            Assert.IsTrue(channelMatcher.DoesMatch(channelName, providerName));
        }

        [TestCase("Cromtel", "Cmrotel", "Cmtel")]
        [Test]
        public void DoesMatch_NamesDoNotMatch_ReturnsFalse(
            string definedName,
            string alias,
            string providerName)
        {
            ChannelName channelName = GetChannelName(definedName, alias);

            Assert.IsFalse(channelMatcher.DoesMatch(channelName, providerName));
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

        [TestCase("|RO| Ardeal TV", "ARDEALTV")]
        [TestCase("|ROM|: Cromtel", "CROMTEL")]
        [TestCase("RO\" Romania TV", "ROMANIATV")]
        [TestCase("RO: Animal World [768p]", "ANIMALWORLD")]
        [TestCase("RUMANIA: DigiWorld FHD (Opt-1)", "DIGIWORLD")]
        [TestCase("U TV", "UTV")]
        [TestCase("VIP|RO|: Discovery Channel FHD", "DISCOVERYCHANNEL")]
        [Test]
        public void NormaliseName_ReturnsExpectedValue(string inputValue, string expectedValue)
        {
            string actualValue = channelMatcher.NormaliseName(inputValue);
            
            Assert.AreEqual(expectedValue, actualValue);
        }

        private ChannelName GetChannelName(string definedName, string alias)
        {
            if (alias is null)
            {
                return new ChannelName(definedName);
            }
            
            return new ChannelName(definedName, alias);
        }
    }
}
