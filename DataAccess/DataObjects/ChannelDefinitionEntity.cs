using System.Collections.Generic;

namespace IptvPlaylistFetcher.DataAccess.DataObjects
{
    public sealed class ChannelDefinitionEntity
    {
        public string Id { get; set; }

        public bool IsEnabled { get; set; }

        public string Name { get; set; }

        public string Group { get; set; }

        public string LogoUrl { get; set; }

        public List<string> Aliases { get; set; }
    }
}
