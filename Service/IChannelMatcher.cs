using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public interface IChannelMatcher
    {
        bool DoChannelNamesMatch(ChannelName name1, string name2);
    }
}
