using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public interface ICacheManager
    {
        void StoreNormalisedChannelName(string name, string normalisedName);
        string GetNormalisedChannelName(string name);

        void StoreStreamStatus(MediaStreamStatus status);
        MediaStreamStatus GetStreamStatus(string url);
    }
}
