using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service
{
    public interface IPlaylistFileBuilder
    {
        string BuildFile(Playlist playlist);

        Playlist ParseFile(string file);
    }
}
