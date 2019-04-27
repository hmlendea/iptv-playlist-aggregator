using System.Collections.Generic;

namespace IptvPlaylistFetcher.Service.Models
{
    public sealed class ChannelDefinition
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public string LogoUrl { get; set; }

        public List<string> Aliases { get; set; }
    }
}
