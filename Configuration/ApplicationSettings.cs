namespace IptvPlaylistAggregator.Configuration
{
    public sealed class ApplicationSettings
    {
        public string OutputPlaylistPath { get; set; }

        public int DaysToCheck { get; set; }

        public bool CanIncludeUnmatchedChannels { get; set; }

        public bool AreTvGuideTagsEnabled { get; set; }
    }
}
