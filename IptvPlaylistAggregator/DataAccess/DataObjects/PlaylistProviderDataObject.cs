using System.Xml.Serialization;

namespace IptvPlaylistAggregator.DataAccess.DataObjects
{
    public sealed class PlaylistProviderDataObject : EntityBase
    {
        public bool IsEnabled { get; set; }

        public int Priority { get; set; }

        [XmlElement("AllowCaching")]
        public bool IsCachingEnabled { get; set; }

        public string Name { get; set; }

        public string UrlFormat { get; set; }

        public string Country { get; set; }

        public string ChannelNameOverride { get; set; }

        public PlaylistProviderDataObject()
        {
            Priority = int.MaxValue;
            IsCachingEnabled = true;
        }
    }
}
