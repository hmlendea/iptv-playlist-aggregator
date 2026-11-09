namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class ChannelDefinition
    {
        public string Id { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        public ChannelName Name { get; set; } = new(string.Empty);

        public string? Country { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;

        public string LogoUrl { get; set; } = string.Empty;
    }
}
