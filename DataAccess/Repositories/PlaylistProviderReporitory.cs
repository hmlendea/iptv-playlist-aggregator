using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;

namespace IptvPlaylistAggregator.DataAccess.Repositories
{
    public sealed class PlaylistProviderRepository : IPlaylistProviderRepository
    {
        readonly ApplicationSettings settings;

        public PlaylistProviderRepository(ApplicationSettings settings)
        {
            this.settings = settings;
        }

        public IEnumerable<PlaylistProviderEntity> GetAll()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<PlaylistProviderEntity>));
            IEnumerable<PlaylistProviderEntity> entities;

            using (TextReader reader = new StreamReader(settings.PlaylistProviderStorePath))
            {
                entities = (IEnumerable<PlaylistProviderEntity>)serializer.Deserialize(reader);
            }

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
    }
}
