using System.Collections.Generic;

using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service
{
    public interface IPlaylistFetcher
    {
        IEnumerable<Playlist> FetchProviderPlaylists(IEnumerable<PlaylistProvider> providers);

        Playlist FetchProviderPlaylist(PlaylistProvider provider);
    }
}
