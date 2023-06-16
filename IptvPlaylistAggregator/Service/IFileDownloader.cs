using System.Threading.Tasks;

namespace IptvPlaylistAggregator.Service
{
    public interface IFileDownloader
    {
        Task<string> TryDownloadStringAsync(string url);
    }
}
