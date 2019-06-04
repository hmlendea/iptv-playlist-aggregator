using System;
using System.Collections.Generic;
using System.Linq;

using NuciDAL.Repositories;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;

namespace IptvPlaylistAggregator.DataAccess.Repositories
{
    public sealed class GroupRepository : XmlRepository<GroupEntity>, IGroupRepository
    {
        public GroupRepository(DataStoreSettings dataStoreSettings)
            : base(dataStoreSettings.GroupStorePath)
        {
        }

        public override IEnumerable<GroupEntity> GetAll()
        {
            IEnumerable<GroupEntity> entities = base.GetAll();

            // TODO: This is a very ugly, yet very quick fix
            foreach (GroupEntity entity in entities)
            {
                if (entity.Priority <= 0)
                {
                    entity.Priority = 99999;
                }
            }

            return entities.OrderBy(x => x.Priority);
        }

        public override void Update(GroupEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
