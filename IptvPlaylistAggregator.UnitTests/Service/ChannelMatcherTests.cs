using NUnit.Framework;

using Moq;

using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.UnitTests.Service
{
    public sealed class ChannelMatcherTests
    {
        private Mock<ICacheManager> cacheMock;

        private IChannelMatcher channelMatcher;

        [SetUp]
        public void SetUp()
        {
            cacheMock = new Mock<ICacheManager>();

            channelMatcher = new ChannelMatcher(cacheMock.Object);
        }

        [TestCase("Diaspora Media", "MD", "MD: Diaspora Media", "MD: Diaspora Media", "MD")]
        [TestCase("INVALID_CHANNEL", null, "iptvcat.com", "iptvcat.com", "RO")]
        [TestCase("INVALID_CHANNEL", null, "iptvcat.com", "iptvcat.com", null)]
        [TestCase("Valea Prahovei TV", "RO", "VP HD", "VP HD", "RO")]
        [TestCase("Valea Prahovei TV", "RO", "VP HD", "VP HD", null)]
        [Test]
        public void ChannelNamesDoMatch_WithAliasWithCountry(
            string definedName,
            string definedCountry,
            string alias,
            string providerName,
            string providerCountry)
        {
            ChannelName channelName = GetChannelName(definedName, definedCountry, alias);

            Assert.That(channelMatcher.DoesMatch(channelName, providerName, providerCountry));
        }

        [TestCase("Agro TV", "RO: Agro", "Agro RO")]
        [TestCase("Agro TV", "RO: Agro", "Agro RO")]
        [TestCase("Antena 1", "RO: Antenna", "RO: Antenna HD")]
        [TestCase("Ardeal TV", "RO: Ardeal TV", "|RO| Ardeal TV")]
        [TestCase("Bollywood Classic", "RO: Bollywood Classic TV", "Bollywood Classic TV VIP RO")]
        [TestCase("Cartoon Network", "RO: Cartoon Network", "VIP|RO|: Cartoon Network")]
        [TestCase("CineMAX 1", "RO: CineMAX", "CineMAX RO")]
        [TestCase("Digi Sport 2", "RO: Digi Sport 2", "RO: DIGI Sport 2")]
        [TestCase("Digi Sport 2", "RO: Digi Sport 2", "Romanian:DIGI Sport 2")]
        [TestCase("Digi World", "RO: Digi World FHD", "RUMANIA: DigiWorld FHD (Opt-1)")]
        [TestCase("Duna", "RO: Duna TV", "RO | Duna Tv")]
        [TestCase("Golf Channel", "FR: Golf Channel", "|FR| GOLF CHANNEL FHD")]
        [TestCase("H!T Music Channel", "RO: Hit Music Channel", "RO: Hit Music Channel")]
        [TestCase("H!T Music Channel", "RO: Hit Music Channel", "RO(L): HIT MUSIC CHANNEL SD")]
        [TestCase("H!T Music Channel", "RO: Hit", "RO | HIT")]
        [TestCase("HBO 1", "RO: HBO", "RO:HBO HD")]
        [TestCase("HD Net Van Damme", "HD NET Jean Claude Van Damme", "HD NET Jean Claude van Damme")]
        [TestCase("Hora TV", "RO: Hora TV", "RO(L): HORA TV SD")]
        [TestCase("Jurnal TV", "MD: Jurnal TV", "Jurnal TV Moldavia")]
        [TestCase("MegaMax", "RO: MegaMax", "RO: MegaMax-HD")]
        [TestCase("NCN TV", "RO: NCN", "RO: NCN HD")]
        [TestCase("Pro TV News", "RO: Pro News", "Pro News")]
        [TestCase("Publika TV", "MD: Publika", "PUBLIKA_TV_HD")]
        [TestCase("Realitatea Plus", "RO: Realitatea TV Plus", "RO(L): REALITATEA TV PLUS SD")]
        [TestCase("România TV", "România TV", "RO\" Romania TV")]
        [TestCase("Somax", "RO: Somax TV", "Somax TV")]
        [TestCase("Sundance", "RO: Sundance TV", "RO: Sundance TV FHD (MultiSub)")]
        [TestCase("Sundance", "RO: Sundance TV", "RO: Sundance TV FHD [Multi-Sub]")]
        [TestCase("Travel Channel", "RO: Travel", "RO | Travel")]
        [TestCase("Travel Mix", "RO: Travel Mix TV", "Travel Mix TV RO")]
        [TestCase("TV Paprika", "RO: Paprika TV", "RO TV Paprika")]
        [TestCase("TV8", "MD: TV8", "TV 8 Moldova HD")]
        [TestCase("TVC21", "MD: TVC21", "TVC 21 Moldova")]
        [TestCase("TVR Moldova", "RO: TVR Moldova", "RO: TVR Moldova")]
        [TestCase("TVR Târgu Mureș", "RO: TVR T?rgu-Mure?", "TVR: Targu Mureș")]
        [TestCase("VSV De Niro", "VSV Robert de Niro", "VSV Robert de Niro HD")]
        [Test]
        public void ChannelNamesDoMatch_WithAliasWithoutCountry(
            string definedName,
            string alias,
            string providerName)
        {
            ChannelName channelName = GetChannelName(definedName, alias);

            Assert.That(channelMatcher.DoesMatch(channelName, providerName, country2: null));
        }

        [TestCase("AMC", "RO: AMC Romania")]
        [TestCase("Antena 3", "Antena 3 Ultra_HD")]
        [TestCase("Elita TV", "Elita TV")]
        [TestCase("Exploris", "Exploris (576p) [Not 24/7]")]
        [TestCase("HBO 3", "HBO 3 F_HD")]
        [TestCase("MTV Europe", "RO: MTV Europe")]
        [TestCase("Pro TV", "PRO TV ULTRA_HD")]
        [TestCase("Realitatea Plus", "Realitatea Plus")]
        [TestCase("TVR 1", "TVR1 [B] RO")]
        [TestCase("TVR", "RO: TVR HD (1080P)")]
        [TestCase("U TV", "UTV")]
        [TestCase("Vivid TV", "Vivid TV HD(18+)")]
        [Test]
        public void ChannelNamesDoMatch_WithoutAliasWithoutCountry(
            string definedName,
            string providerName)
        {
            ChannelName channelName = GetChannelName(definedName, alias: null);

            Assert.That(channelMatcher.DoesMatch(channelName, providerName, country2: null));
        }

        [TestCase("Cromtel", "Cmrotel", "Cmtel")]
        [TestCase("Telekom Sport 2", "RO: Telekom Sport 2", "RO: Digi Sport 2")]
        [Test]
        public void ChannelNamesDoNotMatch_WithAliasWithoutCountry(
            string definedName,
            string alias,
            string providerName)
        {
            ChannelName channelName = GetChannelName(definedName, alias);

            Assert.That(!channelMatcher.DoesMatch(channelName, providerName, country2: null));
        }

        [TestCase("Pro TV", "MD: Pro TV")]
        [TestCase("Pro TV", "MD: ProTV Chisinau")]
        [Test]
        public void ChannelNamesDoNotMatch_WithoutAliasWithoutCountry(
            string definedName,
            string providerName)
        {
            // Arrange
            ChannelName channelName = GetChannelName(definedName, alias: null);

            // Act
            bool isMatch = channelMatcher.DoesMatch(channelName, providerName, country2: null);

            // Assert
            Assert.That(isMatch, Is.False);
        }

        [TestCase(" MD| Publika", "MD", "MDPUBLIKA")]
        [TestCase("|AR| AD SPORT 4 HEVC", "AR", "ARADSPORT4")]
        [TestCase("|FR| GOLF CHANNELS HD", "FR", "FRGOLFCHANNELS")]
        [TestCase("|RO| Ardeal TV", "RO", "ARDEALTV")]
        [TestCase("|ROM|: Cromtel", "RO", "CROMTEL")]
        [TestCase("|UK| CHELSEA TV (Live On Matches) HD", "UK", "UKCHELSEATV")]
        [TestCase("Alfa &amp; Omega RO", "RO", "ALFAOMEGA")]
        [TestCase("Bollywood VIP RO", "RO", "BOLLYWOOD")]
        [TestCase("Canal Regional (Moldova)", "MD", "MDCANALREGIONAL")]
        [TestCase("Crime &amp; Investigation RO", "RO", "CRIMEINVESTIGATION")]
        [TestCase("iConcert FHD RO", "RO", "ICONCERT")]
        [TestCase("MD: MD: Diaspora Media", "MD", "MDDIASPORAMEDIA")]
        [TestCase("RO | Travel", "RO", "TRAVEL")]
        [TestCase("RO: Travel", "RO", "TRAVEL")]
        [TestCase("RO(L): TELEKOM SPORT 1 FHD", "RO", "TELEKOMSPORT1")]
        [TestCase("Telekom Sport 5 FHD [Match Time] RO", "RO", "TELEKOMSPORT5")]
        [TestCase("Travel Mix", "RO", "TRAVELMIX")]
        [TestCase("TV Paprika", "RO", "TVPAPRIKA")]
        [TestCase("TV8", "MD", "MDTV8")]
        [TestCase("TVC21", "MD", "MDTVC21")]
        [TestCase("TVR Moldova", "MD", "MDTVR")]
        [TestCase("TVR Târgu Mureș", "RO", "TVRTARGUMURES")]
        [TestCase("TVR1 [B] RO", "RO", "TVR1")]
        [TestCase("VP HD", "RO", "VP")]
        [TestCase("VSV De Niro", "RO", "VSVDENIRO")]
        [Test]
        public void NormaliseName_WithCountry_ReturnsExpectedValue(string name, string country, string expectedNormalisedName)
        {
            string actualNormalisedName = channelMatcher.NormaliseName(name, country);

            Assert.That(actualNormalisedName, Is.EqualTo(expectedNormalisedName));
        }

        [TestCase(" MD| Publika", "MDPUBLIKA")]
        [TestCase("|AR| AD SPORT 4 HEVC", "ARADSPORT4")]
        [TestCase("|FR| GOLF CHANNELS HD", "FRGOLFCHANNELS")]
        [TestCase("|RO| Ardeal TV", "ARDEALTV")]
        [TestCase("|ROM|: Cromtel", "CROMTEL")]
        [TestCase("|UK| CHELSEA TV (Live On Matches) HD", "UKCHELSEATV")]
        [TestCase("Canal Regional (Moldova)", "MDCANALREGIONAL")]
        [TestCase("Cartoon Network FullHD", "CARTOONNETWORK")]
        [TestCase("Digi 4K", "DIGI4K")]
        [TestCase("DIGI SPORT 4 (RO)", "DIGISPORT4")]
        [TestCase("Jurnal TV Moldova", "MDJURNALTV")]
        [TestCase("MD: Canal Regional (Moldova)", "MDCANALREGIONAL")]
        [TestCase("MD: MD: [MD] Publika", "MDPUBLIKA")]
        [TestCase("MD: MD: Moldova 1", "MDMOLDOVA1")]
        [TestCase("MD: MD| Pro TV Chișinău.", "MDPROTVCHISINAU")]
        [TestCase("MD: ProTV Chisinau", "MDPROTVCHISINAU")]
        [TestCase("MINIMAX ROMANIA HD", "MINIMAXROMANIA")]
        [TestCase("Pro Cinema Full-HD", "PROCINEMA")]
        [TestCase("Pro TV [B] RO", "PROTV")]
        [TestCase("PUBLIKA_TV_HD", "PUBLIKATV")]
        [TestCase("RO    \" DIGI SPORT 1 HD RO", "DIGISPORT1")]
        [TestCase("RO | Travel", "TRAVEL")]
        [TestCase("RO-Animal Planet HD", "ANIMALPLANET")]
        [TestCase("RO: 1HD", "1HD")]
        [TestCase("RO: Animal World [768p]", "ANIMALWORLD")]
        [TestCase("RO: Bit TV (ROM)", "BITTV")]
        [TestCase("RO: Digi24 (România)", "DIGI24")]
        [TestCase("RO: HBO 3 RO", "HBO3")]
        [TestCase("RO: HBO HD RO", "HBO")]
        [TestCase("RO: MiniMax-HD", "MINIMAX")]
        [TestCase("RO: Nașul TV (New!)", "NASULTV")]
        [TestCase("RO: Nickelodeon (RO)", "NICKELODEON")]
        [TestCase("Ro: Pro TV backup", "PROTV")]
        [TestCase("Ro: Romania TV backup", "ROMANIATV")]
        [TestCase("RO: Tele Moldova", "TELEMOLDOVA")]
        [TestCase("RO: Travel", "TRAVEL")]
        [TestCase("RO: TVR Moldova", "TVRMOLDOVA")]
        [TestCase("RO: U TV [b]", "UTV")]
        [TestCase("RO: U TV [B]", "UTV")]
        [TestCase("RO: U TV S1-1", "UTV")]
        [TestCase("RO:HBO HD", "HBO")]
        [TestCase("RO.| DIGI 24", "DIGI24")]
        [TestCase("RO(L): E! ENTERTAINMENT FHD", "EENTERTAINMENT")]
        [TestCase("RO(L): HIT MUSIC CHANNEL SD", "HITMUSICCHANNEL")]
        [TestCase("RO(L): IASI TV SD", "IASITV")]
        [TestCase("RO(L): KronehitTV FHD", "KRONEHITTV")]
        [TestCase("RO(L): REALITATEA TV PLUS SD", "REALITATEATVPLUS")]
        [TestCase("RO(L): VP SD", "VP")]
        [TestCase("RO\" Romania TV", "ROMANIATV")]
        [TestCase("RO| Antena 3 4K+", "ANTENA3")]
        [TestCase("RO| CINEMA RO.", "CINEMARO")]
        [TestCase("RO| Digi Life 4K+", "DIGILIFE")]
        [TestCase("RO| NGRO", "NGRO")]
        [TestCase("RO| TARAF:HD", "TARAF")]
        [TestCase("RO|DISOVERY_SCIENCE_HD", "DISOVERYSCIENCE")]
        [TestCase("RTR Moldova HD", "MDRTR")]
        [TestCase("RUMANIA: DigiWorld FHD (Opt-1)", "DIGIWORLD")]
        [TestCase("TV 8 HD (Auto)", "TV8")]
        [TestCase("TV 8 Moldova              HD", "MDTV8")]
        [TestCase("TV Centrală Moldova", "MDTVCENTRALA")]
        [TestCase("TVR 1 (Backup) RO", "TVR1")]
        [TestCase("TVR2 [B] RO", "TVR2")]
        [TestCase("U TV", "UTV")]
        [TestCase("US: NASA TV US", "USNASATV")]
        [TestCase("Viasat Explore Full_HD", "VIASATEXPLORE")]
        [TestCase("VIP|RO|: Discovery Channel FHD", "DISCOVERYCHANNEL")]
        [TestCase("VSV Robert de Niro HD", "VSVROBERTDENIRO")]
        [TestCase("VSV Robert de Niro", "VSVROBERTDENIRO")]
        [TestCase("ZonaM Moldova", "MDZONAM")]
        [Test]
        public void NormaliseName_WithoutCountry_ReturnsExpectedValue(string inputValue, string expectedValue)
        {
            string actualValue = channelMatcher.NormaliseName(inputValue, country: null);

            Assert.That(expectedValue, Is.EqualTo(actualValue));
        }

        private ChannelName GetChannelName(string definedName, string alias)
            => GetChannelName(definedName, country: null, alias);

        private static ChannelName GetChannelName(string definedName, string country, string alias)
        {
            if (alias is null)
            {
                return new ChannelName(definedName);
            }

            return new ChannelName(definedName, country, alias);
        }
    }
}
