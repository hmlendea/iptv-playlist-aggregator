namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class Channel
    {
        public string Id { get; set; }
        
        public string Name { get; set; }
        
        public string Group { get; set; }
        
        public string Country { get; set; }

        public string LogoUrl { get; set; }

        public int Number { get; set; }

        public string Url { get; set; }
    }
}