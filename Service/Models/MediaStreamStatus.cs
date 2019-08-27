using System;

namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class MediaStreamStatus
    {
        public string Url { get; set; }

        public bool IsAlive { get; set; }

        public DateTime LastCheckTime { get; set; }
    }
}
