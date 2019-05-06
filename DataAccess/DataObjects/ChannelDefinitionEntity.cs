using System.Collections.Generic;

namespace IptvPlaylistAggregator.DataAccess.DataObjects
{
    public sealed class ChannelDefinitionEntity
    {
        public string Id { get; set; }

        public bool IsEnabled { get; set; }

        public string Name { get; set; }

        public string GroupId { get; set; }

        public string LogoUrl { get; set; }

        public List<string> Aliases { get; set; }
    }
}
