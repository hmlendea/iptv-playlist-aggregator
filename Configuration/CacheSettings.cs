namespace IptvPlaylistAggregator.Configuration
{
    public sealed class CacheSettings
    {
        public string CacheDirectoryPath { get; set; }

        public int HostCacheTimeout { get; set; }

        public int StreamAliveStatusCacheTimeout { get; set; }

        public int StreamDeadStatusCacheTimeout { get; set; }

        public int StreamNotFoundStatusCacheTimeout { get; set; }
    }
}
