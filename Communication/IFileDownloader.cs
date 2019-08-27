using System;
using System.Net;

namespace IptvPlaylistAggregator.Communication
{
    public interface IFileDownloader
    {
        string DownloadString(string url);

        string TryDownloadString(string url);
    }
}
