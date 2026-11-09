namespace IptvPlaylistAggregator.Configuration
{
    public sealed class CacheSettings
    {
        public string CacheDirectoryPath { get; set; } = string.Empty;

        public int StreamAliveStatusCacheTimeout { get; set; }

        public int StreamDeadStatusCacheTimeout { get; set; }

        public int StreamUnauthorisedStatusCacheTimeout { get; set; }

        public int StreamNotFoundStatusCacheTimeout { get; set; }
    }
}
