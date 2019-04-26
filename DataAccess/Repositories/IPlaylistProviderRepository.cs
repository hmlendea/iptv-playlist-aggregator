using System.Collections.Generic;

using IptvPlaylistFetcher.DataAccess.DataObjects;

namespace IptvPlaylistFetcher.DataAccess.Repositories
{
    public interface IPlaylistProviderRepository
    {
        IEnumerable<PlaylistProviderEntity> GetAll();
    }
}
