namespace IptvPlaylistAggregator.Configuration
{
    public sealed class CacheSettings
    {
        public string CacheDirectoryPath { get; set; }

        public string MediaStreamAliveStatusCacheFileName { get; set; }

        public int MediaStreamStatusCacheTimeoutMins { get; set; }
    }
}
