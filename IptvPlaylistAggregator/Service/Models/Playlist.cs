using System.Collections.Generic;

using NuciExtensions;

namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class Playlist
    {
        public List<Channel> Channels { get; } = [];

        public bool IsEmpty => EnumerableExt.IsNullOrEmpty(Channels);

        public static bool IsNullOrEmpty(Playlist playlist)
            => playlist is null || playlist.IsEmpty;
    }
}
