using System;
using System.Collections.Generic;

using NuciDAL.Repositories;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;

namespace IptvPlaylistAggregator.DataAccess.Repositories
{
    public sealed class ChannelDefinitionRepository : XmlRepository<ChannelDefinitionEntity>, IChannelDefinitionRepository
    {
        const string UnknownGroupPlaceholder = "unknown";

        public ChannelDefinitionRepository(DataStoreSettings dataStoreSettings)
            : base(dataStoreSettings.ChannelStorePath)
        {
        }

        public override IEnumerable<ChannelDefinitionEntity> GetAll()
        {
            IEnumerable<ChannelDefinitionEntity> entities = base.GetAll();

            foreach (ChannelDefinitionEntity channelDef in entities)
            {
                if (string.IsNullOrWhiteSpace(channelDef.GroupId))
                {
                    channelDef.GroupId = UnknownGroupPlaceholder;
                }
            }

            return entities;
        }

        public override void Update(ChannelDefinitionEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
