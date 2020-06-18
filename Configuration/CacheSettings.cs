namespace IptvPlaylistAggregator.Configuration
{
    public sealed class CacheSettings
    {
        public string CacheDirectoryPath { get; set; }

        public int HostCacheTimeout { get; set; }

        public int StreamStatusCacheTimeout { get; set; }
    }
}
