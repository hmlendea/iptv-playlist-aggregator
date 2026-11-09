namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class Group
    {
        public string Id { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Priority { get; set; }
    }
}
