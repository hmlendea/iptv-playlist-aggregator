using System.Threading.Tasks;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public interface IMediaSourceChecker
    {
        Task<bool> IsSourcePlayableAsync(string url);
    }
}
