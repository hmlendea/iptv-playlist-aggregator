namespace IptvPlaylistFetcher.Service
{
    public interface IMediaStreamStatusChecker
    {
        bool IsStreamAlive(string url);
    }
}
