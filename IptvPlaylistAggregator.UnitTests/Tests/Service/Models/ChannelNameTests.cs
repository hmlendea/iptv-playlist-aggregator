using NUnit.Framework;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.UnitTests.Service.Models
{
    public sealed class ChannelNameTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Equals_CompareWithSameValue_ReturnsTrue()
        {
            string definedName = "Digi Sport 2";
            string providerName = "RO: Digi Sport 2";
            string alias = "RO: Digi Sport 2";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsTrue(channelName.Equals(providerName));
        }

        [Test]
        public void Equals_CompareWithSameValueDifferentCasing_ReturnsTrue()
        {
            string definedName = "Digi Sport 2";
            string providerName = "RO: Digi Sport 2";
            string alias = "RO: DIGI Sport 2";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsTrue(channelName.Equals(providerName));
        }

        [Test]
        public void Equals_CompareWithSameValueWithDiacritics_ReturnsTrue()
        {
            string definedName = "TVR Timișoara";
            string providerName = "RO: TVR Timisoara";
            string alias = "RO: TVR Timișoara";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsTrue(channelName.Equals(providerName));
        }

        [Test]
        public void Equals_CompareWithSameValueWithBlacklistedSubstrings_ReturnsTrue()
        {
            string definedName = "Digi Sport 2";
            string providerName = "RO: Digi Sport 2 [Multi-Audio]";
            string alias = "RO: Digi Sport 2";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsTrue(channelName.Equals(providerName));
        }

        [Test]
        public void Equals_CompareWithDifferentValue_ReturnsFalse()
        {
            string definedName = "Digi Sport 2";
            string providerName = "RO: Digi Sport 2";
            string alias = "RO: Telekom Sport 2";

            ChannelName channelName = new ChannelName(definedName, alias);

            Assert.IsFalse(channelName.Equals(providerName));
        }
    }
}
