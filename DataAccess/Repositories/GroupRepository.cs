using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using IptvPlaylistFetcher.Configuration;
using IptvPlaylistFetcher.DataAccess.DataObjects;

namespace IptvPlaylistFetcher.DataAccess.Repositories
{
    public sealed class GroupRepository : IGroupRepository
    {
        readonly ApplicationSettings settings;

        public GroupRepository(ApplicationSettings settings)
        {
            this.settings = settings;
        }

        public IEnumerable<GroupEntity> GetAll()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<GroupEntity>));
            IEnumerable<GroupEntity> entities;

            using (TextReader reader = new StreamReader(settings.GroupStorePath))
            {
                entities = (IEnumerable<GroupEntity>)serializer.Deserialize(reader);
            }

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
    }
}
