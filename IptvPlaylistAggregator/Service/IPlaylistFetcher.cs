using System.Collections.Generic;
using System.Threading.Tasks;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public interface IPlaylistFetcher
    {
        IEnumerable<Playlist> FetchProviderPlaylists(IEnumerable<PlaylistProvider> providers);

        Task<Playlist> FetchProviderPlaylistAsync(PlaylistProvider provider);
    }
}
