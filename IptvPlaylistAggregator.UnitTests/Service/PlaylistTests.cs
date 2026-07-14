using NUnit.Framework;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.UnitTests.Service
{
    [TestFixture]
    public sealed class PlaylistTests
    {
        // -- IsNullOrEmpty ------

        [Test]
        public void GivenNullPlaylist_WhenCheckingIfNullOrEmpty_ThenTrueIsReturned()
            => Assert.That(Playlist.IsNullOrEmpty(null), Is.True);

        [Test]
        public void GivenEmptyPlaylist_WhenCheckingIfNullOrEmpty_ThenTrueIsReturned()
        {
            Playlist playlist = new();

            Assert.That(Playlist.IsNullOrEmpty(playlist), Is.True);
        }

        [Test]
        public void GivenPlaylistWithOneChannel_WhenCheckingIfNullOrEmpty_ThenFalseIsReturned()
        {
            Playlist playlist = new();
            playlist.Channels.Add(new Channel());

            Assert.That(Playlist.IsNullOrEmpty(playlist), Is.False);
        }

        [Test]
        public void GivenPlaylistWithMultipleChannels_WhenCheckingIfNullOrEmpty_ThenFalseIsReturned()
        {
            Playlist playlist = new();
            playlist.Channels.Add(new Channel());
            playlist.Channels.Add(new Channel());

            Assert.That(Playlist.IsNullOrEmpty(playlist), Is.False);
        }

        // -- IsEmpty ------

        [Test]
        public void GivenEmptyPlaylist_WhenCheckingIfEmpty_ThenTrueIsReturned()
        {
            Playlist playlist = new();

            Assert.That(playlist.IsEmpty, Is.True);
        }

        [Test]
        public void GivenPlaylistWithOneChannel_WhenCheckingIfEmpty_ThenFalseIsReturned()
        {
            Playlist playlist = new();
            playlist.Channels.Add(new Channel());

            Assert.That(playlist.IsEmpty, Is.False);
        }

        [Test]
        public void GivenPlaylistWithMultipleChannels_WhenCheckingIfEmpty_ThenFalseIsReturned()
        {
            Playlist playlist = new();
            playlist.Channels.Add(new Channel());
            playlist.Channels.Add(new Channel());

            Assert.That(playlist.IsEmpty, Is.False);
        }

        [Test]
        public void GivenEmptyPlaylistAfterAddingChannel_WhenCheckingIfEmpty_ThenFalseIsReturned()
        {
            Playlist playlist = new();

            Assert.That(playlist.IsEmpty, Is.True);

            playlist.Channels.Add(new Channel());

            Assert.That(playlist.IsEmpty, Is.False);
        }
    }
}
