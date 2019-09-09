namespace IptvPlaylistAggregator.Service
{
    public interface IDnsResolver
    {
        string ResolveHostname(string hostname);

        string ResolveUrl(string url);
    }
}
