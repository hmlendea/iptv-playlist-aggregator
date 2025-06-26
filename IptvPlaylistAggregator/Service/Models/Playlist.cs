using System.Collections.Generic;
using NuciExtensions;

namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class Playlist
    {
        public IList<Channel> Channels;

        public bool IsEmpty => EnumerableExt.IsNullOrEmpty(Channels);

        public Playlist() => Channels = [];

        public static bool IsNullOrEmpty(Playlist playlist)
            => playlist is null || playlist.IsEmpty;
    }
}
