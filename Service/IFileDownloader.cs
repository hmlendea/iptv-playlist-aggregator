using System.Threading.Tasks;

namespace IptvPlaylistAggregator.Service
{
    public interface IFileDownloader
    {
        string DownloadString(string url);

        string TryDownloadString(string url);

        Task<string> TryDownloadStringTaskAsync(string url);
    }
}
