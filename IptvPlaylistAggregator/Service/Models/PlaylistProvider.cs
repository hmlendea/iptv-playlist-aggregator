namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class PlaylistProvider
    {
        public string Id { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        public int Priority { get; set; }

        public bool IsCachingEnabled { get; set; }

        public string Name { get; set; } = string.Empty;

        public string UrlFormat { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string ChannelNameOverride { get; set; } = string.Empty;
    }
}
