namespace IptvPlaylistAggregator.Configuration
{
    public sealed class ApplicationSettings
    {
        public string OutputPlaylistPath { get; set; } = string.Empty;

        public int DaysToCheck { get; set; }

        public bool AreUnmatchedChannelsIncluded { get; set; }

        public bool AreTvGuideTagsEnabled { get; set; }

        public bool ArePlaylistDetailsTagsEnabled { get; set; }
    }
}
