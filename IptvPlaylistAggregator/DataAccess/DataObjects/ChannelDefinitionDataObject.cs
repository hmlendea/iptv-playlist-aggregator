using System.Collections.Generic;

using NuciDAL.DataObjects;

namespace IptvPlaylistAggregator.DataAccess.DataObjects
{
    public sealed class ChannelDefinitionDataObject : EntityBase
    {
        public bool IsEnabled { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;

        public string LogoUrl { get; set; } = string.Empty;

        public List<string> Aliases { get; set; } = [];

        public ChannelDefinitionDataObject()
        {
            IsEnabled = true;
            GroupId = UnknownGroupPlaceholder;
        }

        private static string UnknownGroupPlaceholder => "unknown";
    }
}
