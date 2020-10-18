using NUnit.Framework;

using Moq;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service;

using NuciLog.Core;

namespace IptvPlaylistAggregator.UnitTests.Service.Models
{
    public sealed class MediaSourceCheckerTests
    {
        Mock<IFileDownloader> fileDownloaderMock;
        Mock<IPlaylistFileBuilder> playlistFileBuilderMock;
        Mock<ICacheManager> cacheMock;
        Mock<ILogger> loggerMock;
        ApplicationSettings applicationSettings;

        IMediaSourceChecker mediaSourceChecker;

        [SetUp]
        public void SetUp()
        {
            fileDownloaderMock = new Mock<IFileDownloader>();
            playlistFileBuilderMock = new Mock<IPlaylistFileBuilder>();
            cacheMock = new Mock<ICacheManager>();
            loggerMock = new Mock<ILogger>();
            applicationSettings = new ApplicationSettings();

            mediaSourceChecker = new MediaSourceChecker(
                fileDownloaderMock.Object,
                playlistFileBuilderMock.Object,
                cacheMock.Object,
                loggerMock.Object,
                applicationSettings);
        }

        [TestCase("https://www.youtube.com/watch?v=fxFIgBld95E")]
        [Test]
        public void GivenTheSourceIsYouTubeVideo_WhenCheckingThatItIsPlayable_ThenFalseIsReturned(string sourceUrl)
        {
            AssertThatSourceUrlIsNotPlayable(sourceUrl);
        }

        [TestCase("https://dotto.edvr.ro/dottotv_1603019528.mp4")]
        [TestCase("https://iptvcat.com/assets/videos/lazycat-iptvcat.com.mp4?fluxustv.m3u8")]
        [TestCase("https://iptvcat.com/assets/videos/lazycat-iptvcat.com.mp4")]
        [Test]
        public void GivenTheSourceIsShortenedUrl_WhenCheckingThatItIsPlayable_ThenFalseIsReturned(string sourceUrl)
        {
            AssertThatSourceUrlIsNotPlayable(sourceUrl);
        }

        [TestCase("https://tinyurl.com/y9k7rbje")]
        [Test]
        public void GivenTheSourceIsAnMP4File_WhenCheckingThatItIsPlayable_ThenFalseIsReturned(string sourceUrl)
        {
            AssertThatSourceUrlIsNotPlayable(sourceUrl);
        }

        [TestCase("mms://86.34.169.52:8080/")]
        [TestCase("mms://musceltvlive.muscel.ro:8080")]
        [Test]
        public void GivenTheSourceIsUsingTeMmsProtocol_WhenCheckingThatItIsPlayable_ThenFalseIsReturned(string sourceUrl)
        {
            AssertThatSourceUrlIsNotPlayable(sourceUrl);
        }


        [TestCase("mmsh://82.137.6.58:1234/")]
        [TestCase("mmsh://musceltvlive.muscel.ro:8080")]
        [Test]
        public void GivenTheSourceIsUsingTeMmshProtocol_WhenCheckingThatItIsPlayable_ThenFalseIsReturned(string sourceUrl)
        {
            AssertThatSourceUrlIsNotPlayable(sourceUrl);
        }

        [TestCase("rtmp://212.0.209.209:1935/live/_definst_mp4:Moldova")]
        [TestCase("rtmp://81.18.66.155/live/banat-tv")]
        [TestCase("rtmp://86.106.82.47/baricadatv_live/livestream")]
        [TestCase("rtmp://89.33.78.174:1935/live/livestream")]
        [TestCase("rtmp://89.33.78.174/live/livestream")]
        [TestCase("rtmp://columna1.arya.ro/live//columnatv1")]
        [TestCase("rtmp://columna1.arya.ro/live/columnatv1")]
        [TestCase("rtmp://direct.multimedianet.ro:1935/live/livestream")]
        [TestCase("rtmp://gtv1.arya.ro:1935/live/gtv1.flv")]
        [TestCase("rtmp://rapsodia1.arya.ro/live//rapsodiatv1")]
        [TestCase("rtmp://rapsodia1.arya.ro/live/rapsodiatv1")]
        [TestCase("rtmp://streaming.tvmures.ro:1935/live/ttm")]
        [TestCase("rtmp://streaming.tvsatrm.ro/live/tvsat")]
        [TestCase("rtmp://traditii1.arya.ro/live/traditiitv1")]
        [TestCase("rtmp://v1.arya.ro:1935/live/ptv1.flv")]
        [Test]
        public void GivenTheSourceIsUsingTeRtmpProtocol_WhenCheckingThatItIsPlayable_ThenFalseIsReturned(string sourceUrl)
        {
            AssertThatSourceUrlIsNotPlayable(sourceUrl);
        }

        [TestCase("rtsp://195.64.178.23/somax")]
        [TestCase("rtsp://195.64.178.23/telem")]
        [TestCase("rtsp://212.0.209.209:1935/live/_definst_/mp4:MoldovaUnu1")]
        [TestCase("rtsp://83.218.202.202:1935/live/wt_publika.stream")]
        [TestCase("rtsp://live.trm.md:1935/live/M1Mlive")]
        [Test]
        public void GivenTheSourceIsUsingTeRtspProtocol_WhenCheckingThatItIsPlayable_ThenFalseIsReturned(string sourceUrl)
        {
            AssertThatSourceUrlIsNotPlayable(sourceUrl);
        }

        void AssertThatSourceUrlIsNotPlayable(string sourceUrl)
        {
            // Act
            bool sourceIsPlayale = mediaSourceChecker.IsSourcePlayableAsync(sourceUrl).Result;

            // Assert
            Assert.That(sourceIsPlayale, Is.False);
        }
    }
}
