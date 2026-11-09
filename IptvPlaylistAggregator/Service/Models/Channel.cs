namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class Channel
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Group { get; set; } = string.Empty;

        public string? Country { get; set; } = string.Empty;

        public string? LogoUrl { get; set; } = string.Empty;

        public int Number { get; set; }

        public string PlaylistId { get; set; } = string.Empty;

        public string PlaylistChannelName { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;
    }
}
