namespace IptvPlaylistFetcher.Core.Configuration
{
    public sealed class ApplicationSettings
    {
        public string ChannelStorePath { get; set; }

        public string PlaylistProviderStorePath { get; set; }

        public string CacheDirectoryPath { get; set; }

        public string MediaStreamAliveStatusCacheFileName { get; set; }

        public int MediaStreamStatusCacheTimeoutMins { get; set; }

        public int DaysToCheck { get; set; }

        public bool AreTvGuideTagsEnabled { get; set; }
    }
}
