using System.Collections.Generic;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public interface IPlaylistFetcher
    {
        IEnumerable<Playlist> FetchProviderPlaylists(IEnumerable<PlaylistProvider> providers);

        Playlist FetchProviderPlaylist(PlaylistProvider provider);
    }
}
