using System;

namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class Host
    {
        public string Domain { get; set; }

        public string Ip { get; set; }

        public DateTime ResolutionTime { get; set; }
    }
}
