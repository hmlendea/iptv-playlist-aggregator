using NuciLog.Core;

namespace IptvPlaylistAggregator.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name)
            : base(name)
        {
            
        }

        public static Operation PlaylistFetching => new MyOperation(nameof(PlaylistFetching));

        public static Operation ProviderChannelsFiltering => new MyOperation(nameof(ProviderChannelsFiltering));

        public static Operation ChannelMatching => new MyOperation(nameof(ChannelMatching));
    }
}
