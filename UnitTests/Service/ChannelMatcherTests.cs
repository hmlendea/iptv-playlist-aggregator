using NUnit.Framework;

using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.UnitTests.Service.Models
{
    public sealed class ChannelMatcherTests
    {
        IChannelMatcher channelMatcher;

        [SetUp]
        public void SetUp()
        {
            channelMatcher = new ChannelMatcher();
        }

        [Test]
        public void DoChannelNamesMatch_CompareWithSameValue_ReturnsTrue()
        {
            string definedName = "Digi Sport 2";
            string providerName = "RO: Digi Sport 2";
            string alias = "RO: Digi Sport 2";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsTrue(channelMatcher.DoChannelNamesMatch(channelName, providerName));
        }

        [Test]
        public void DoChannelNamesMatch_CompareWithSameValueDifferentCasing_ReturnsTrue()
        {
            string definedName = "Digi Sport 2";
            string providerName = "RO: Digi Sport 2";
            string alias = "RO: DIGI Sport 2";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsTrue(channelMatcher.DoChannelNamesMatch(channelName, providerName));
        }

        [Test]
        public void DoChannelNamesMatch_CompareWithSameValueWithDiacritics_ReturnsTrue()
        {
            string definedName = "TVR Timișoara";
            string providerName = "RO: TVR Timisoara";
            string alias = "RO: TVR Timișoara";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsTrue(channelMatcher.DoChannelNamesMatch(channelName, providerName));
        }

        [Test]
        public void DoChannelNamesMatch_CompareWithSameValueWithBlacklistedSubstrings_ReturnsTrue()
        {
            string definedName = "Digi Sport 2";
            string providerName = "RO: Digi Sport 2 [Multi-Audio]";
            string alias = "RO: Digi Sport 2";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsTrue(channelMatcher.DoChannelNamesMatch(channelName, providerName));
        }

        [Test]
        public void DoChannelNamesMatch_CompareWithDifferentValue_ReturnsFalse()
        {
            string definedName = "Digi Sport 2";
            string providerName = "RO: Digi Sport 2";
            string alias = "RO: Telekom Sport 2";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsFalse(channelMatcher.DoChannelNamesMatch(channelName, providerName));
        }
    }
}
