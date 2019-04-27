using System.Collections.Generic;

namespace IptvPlaylistFetcher.Service.Models
{
    public sealed class Playlist
    {
        public IList<Channel> Channels;

        public bool IsEmpty => Channels is null || Channels.Count == 0;

        public Playlist()
        {
            this.Channels = new List<Channel>();
        }

        public static bool IsNullOrEmpty(Playlist playlist)
            => playlist is null || playlist.IsEmpty;
    }
}
