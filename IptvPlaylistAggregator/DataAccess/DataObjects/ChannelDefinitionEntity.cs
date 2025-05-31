using System.Collections.Generic;

using NuciDAL.DataObjects;

namespace IptvPlaylistAggregator.DataAccess.DataObjects
{
    public sealed class ChannelDefinitionEntity : EntityBase
    {
        private const string UnknownGroupPlaceholder = "unknown";

        public bool IsEnabled { get; set; }

        public string Name { get; set; }

        public string Country { get; set; }

        public string GroupId { get; set; }

        public string LogoUrl { get; set; }

        public List<string> Aliases { get; set; }

        public ChannelDefinitionEntity()
        {
            IsEnabled = true;
            GroupId = UnknownGroupPlaceholder;
        }
    }
}
