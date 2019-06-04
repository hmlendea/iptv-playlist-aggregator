using System;
using System.Collections.Generic;
using System.Linq;

using NuciDAL.Repositories;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;

namespace IptvPlaylistAggregator.DataAccess.Repositories
{
    public sealed class PlaylistProviderRepository : XmlRepository<PlaylistProviderEntity>, IPlaylistProviderRepository
    {
        public PlaylistProviderRepository(DataStoreSettings dataStoreSettings)
            : base(dataStoreSettings.PlaylistProviderStorePath)
        {
        }

        public override IEnumerable<PlaylistProviderEntity> GetAll()
        {
            IEnumerable<PlaylistProviderEntity> entities = base.GetAll();

            // TODO: This is a very ugly, yet very quick fix
            foreach (PlaylistProviderEntity entity in entities)
            {
                if (entity.Priority <= 0)
                {
                    entity.Priority = 99999;
                }
            }

            return entities.OrderBy(x => x.Priority);
        }

        public override void Update(PlaylistProviderEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
