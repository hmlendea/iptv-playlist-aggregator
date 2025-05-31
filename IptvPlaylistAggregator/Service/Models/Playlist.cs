using System.Collections.Generic;

namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class Playlist
    {
        public IList<Channel> Channels;

        public bool IsEmpty => Channels is null || Channels.Count == 0;

        public Playlist() => Channels = [];

        public static bool IsNullOrEmpty(Playlist playlist)
            => playlist is null || playlist.IsEmpty;
    }
}
