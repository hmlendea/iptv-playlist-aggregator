using System.Collections.Generic;

namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class ChannelName
    {
        public string Value { get; set; }

        public IEnumerable<string> Aliases { get; set; }

        public ChannelName(string name)
            : this(name, new List<string>())
        {
            
        }

        public ChannelName(string name, IEnumerable<string> aliases)
        {
            Value = name;
            Aliases = aliases;
        }

        public ChannelName(string name, params string[] aliases)
        {
            Value = name;
            Aliases = aliases;
        }
    }
}
