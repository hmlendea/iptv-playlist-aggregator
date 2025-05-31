using NuciLog.Core;

namespace IptvPlaylistAggregator.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        private MyLogInfoKey(string name) : base(name) { }

        public static LogInfoKey Channel => new MyLogInfoKey(nameof(Channel));

        public static LogInfoKey ChannelsCount => new MyLogInfoKey(nameof(ChannelsCount));

        public static LogInfoKey Group => new MyLogInfoKey(nameof(Group));

        public static LogInfoKey Provider => new MyLogInfoKey(nameof(Provider));

        public static LogInfoKey Url => new MyLogInfoKey(nameof(Url));

        public static LogInfoKey StreamState => new MyLogInfoKey(nameof(StreamState));
    }
}
