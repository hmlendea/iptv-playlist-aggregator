using System.Collections.Generic;

namespace IptvPlaylistFetcher.Service.Models
{
    public sealed class Playlist
    {
        public IEnumerable<Channel> Channels;

        public Playlist()
        {
            this.Channels = new List<Channel>();
        }
    }
}
