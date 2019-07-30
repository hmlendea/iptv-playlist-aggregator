using System.Collections.Generic;

namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class ChannelDefinition
    {
        public string Id { get; set; }

        public bool IsEnabled { get; set; }

        public ChannelName Name { get; set; }

        public string GroupId { get; set; }

        public string LogoUrl { get; set; }
    }
}
