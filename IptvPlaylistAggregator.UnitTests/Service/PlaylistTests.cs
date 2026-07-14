using NUnit.Framework;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.UnitTests.Service
{
    public sealed class PlaylistTests
    {
        [Test]
        public void IsNullOrEmpty_GivenNull_ThenTrueIsReturned()
        {
            Assert.That(Playlist.IsNullOrEmpty(null), Is.True);
        }

        [Test]
        public void IsNullOrEmpty_GivenEmptyPlaylist_ThenTrueIsReturned()
        {
            Playlist playlist = new();

            Assert.That(Playlist.IsNullOrEmpty(playlist), Is.True);
        }

        [Test]
        public void IsNullOrEmpty_GivenPlaylistWithChannels_ThenFalseIsReturned()
        {
            Playlist playlist = new();
            playlist.Channels.Add(new Channel());

            Assert.That(Playlist.IsNullOrEmpty(playlist), Is.False);
        }

        [Test]
        public void IsNullOrEmpty_GivenPlaylistWithMultipleChannels_ThenFalseIsReturned()
        {
            Playlist playlist = new();
            playlist.Channels.Add(new Channel());
            playlist.Channels.Add(new Channel());

            Assert.That(Playlist.IsNullOrEmpty(playlist), Is.False);
        }

        [Test]
        public void IsEmpty_GivenEmptyPlaylist_ThenTrueIsReturned()
        {
            Playlist playlist = new();

            Assert.That(playlist.IsEmpty, Is.True);
        }

        [Test]
        public void IsEmpty_GivenPlaylistWithSingleChannel_ThenFalseIsReturned()
        {
            Playlist playlist = new();
            playlist.Channels.Add(new Channel());

            Assert.That(playlist.IsEmpty, Is.False);
        }

        [Test]
        public void IsEmpty_GivenPlaylistWithMultipleChannels_ThenFalseIsReturned()
        {
            Playlist playlist = new();
            playlist.Channels.Add(new Channel());
            playlist.Channels.Add(new Channel());

            Assert.That(playlist.IsEmpty, Is.False);
        }

        [Test]
        public void IsEmpty_AfterAddingChannel_ThenFalseIsReturned()
        {
            Playlist playlist = new();

            Assert.That(playlist.IsEmpty, Is.True);

            playlist.Channels.Add(new Channel());

            Assert.That(playlist.IsEmpty, Is.False);
        }
    }
}
