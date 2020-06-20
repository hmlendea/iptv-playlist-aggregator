using System;

namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class MediaStreamStatus
    {
        public string Url { get; set; }

        public StreamState State { get; set; }

        public DateTime LastCheckTime { get; set; }
    }
}
