using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public interface IChannelMatcher
    {
        string NormaliseName(string name);

        bool DoesMatch(ChannelName name1, string name2);
    }
}
