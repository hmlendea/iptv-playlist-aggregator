namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class PlaylistProvider
    {
        public string Id { get; set; }

        public bool IsEnabled { get; set; }

        public int Priority { get; set; }

        public bool IsCachingEnabled { get; set; }

        public string Name { get; set; }

        public string UrlFormat { get; set; }

        public string Country { get; set; }

        public string ChannelNameOverride { get; set; }
    }
}
