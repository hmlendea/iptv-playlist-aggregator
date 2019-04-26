using System.Collections.Generic;

using IptvPlaylistFetcher.DataAccess.DataObjects;

namespace IptvPlaylistFetcher.DataAccess.Repositories
{
    public interface IChannelRepository
    {
        IEnumerable<ChannelEntity> GetAll();
    }
}
