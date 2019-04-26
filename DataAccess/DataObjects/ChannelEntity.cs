using System.Collections.Generic;

namespace IptvPlaylistFetcher.DataAccess.DataObjects
{
    public sealed class ChannelEntity
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public List<string> Aliases { get; set; }
    }
}
