namespace IptvPlaylistFetcher.Service
{
    public interface IMediaSourceChecker
    {
        bool IsSourcePlayable(string url);
    }
}
