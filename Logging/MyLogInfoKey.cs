using NuciLog.Core;

namespace IptvPlaylistAggregator.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        protected MyLogInfoKey(string name)
            : base(name)
        {
            
        }

        public static LogInfoKey Url => new MyLogInfoKey(nameof(Url));

        public static LogInfoKey Channel => new MyLogInfoKey(nameof(Channel));

        public static LogInfoKey ChannelsCount => new MyLogInfoKey(nameof(ChannelsCount));

        public static LogInfoKey Group => new MyLogInfoKey(nameof(Group));
    }
}