using System.Threading.Tasks;

namespace IptvPlaylistAggregator.Service
{
    public interface IMediaSourceChecker
    {
        Task<bool> IsSourcePlayableAsync(string url);
    }
}
