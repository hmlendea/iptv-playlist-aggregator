using System.Threading.Tasks;

using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service
{
    public interface IPlaylistFetcher
    {
        string GetPlaylistFile();
    }
}
