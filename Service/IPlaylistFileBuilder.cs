using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public interface IPlaylistFileBuilder
    {
        string BuildFile(Playlist playlist);

        Playlist ParseFile(string file);
    }
}
