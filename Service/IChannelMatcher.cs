using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public interface IChannelMatcher
    {
        string NormaliseName(string name, string country);

        bool DoesMatch(ChannelName name1, string name2, string country2);
    }
}
