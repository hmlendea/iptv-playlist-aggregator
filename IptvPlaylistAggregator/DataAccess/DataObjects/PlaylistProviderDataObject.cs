using System.Xml.Serialization;

using NuciDAL.DataObjects;

namespace IptvPlaylistAggregator.DataAccess.DataObjects
{
    public sealed class PlaylistProviderDataObject : EntityBase
    {
        public bool IsEnabled { get; set; }

        public int Priority { get; set; }

        [XmlElement("AllowCaching")]
        public bool IsCachingEnabled { get; set; }

        public string Name { get; set; } = string.Empty;

        public string UrlFormat { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string ChannelNameOverride { get; set; } = string.Empty;

        public PlaylistProviderDataObject()
        {
            Priority = int.MaxValue;
            IsCachingEnabled = true;
        }
    }
}
