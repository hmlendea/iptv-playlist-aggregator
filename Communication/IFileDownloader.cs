namespace IptvPlaylistAggregator.Communication
{
    public interface IFileDownloader
    {
        string DownloadString(string url);

        string TryDownloadString(string url);
    }
}
