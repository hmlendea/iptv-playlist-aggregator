namespace IptvPlaylistAggregator.Service
{
    public interface IMediaSourceChecker
    {
        bool IsSourcePlayable(string url);
    }
}
