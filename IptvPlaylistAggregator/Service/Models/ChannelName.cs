namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class ChannelName(string name, string country, IEnumerable<string> aliases)
    {
        public string Value { get; set; } = name;

        public string Country { get; set; } = country;

        public IEnumerable<string> Aliases { get; set; } = aliases;

        public ChannelName(string name) : this(name, country: null) { }

        public ChannelName(string name, string country) : this(name, country, []) { }

        public ChannelName(string name, params string[] aliases) : this(name, country: null, (IEnumerable<string>)aliases) { }

        public ChannelName(string name, string country, params string[] aliases) : this(name, country, (IEnumerable<string>)aliases) { }

        public ChannelName(string name, IEnumerable<string> aliases) : this(name, country: null, aliases) { }
    }
}
