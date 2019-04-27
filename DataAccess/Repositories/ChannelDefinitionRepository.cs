using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using IptvPlaylistFetcher.Core.Configuration;
using IptvPlaylistFetcher.DataAccess.DataObjects;

namespace IptvPlaylistFetcher.DataAccess.Repositories
{
    public sealed class ChannelDefinitionRepository : IChannelDefinitionRepository
    {
        const char CsvFieldSeparator = ',';
        const char CsvCollectionSeparator = '|';
        const string UnknownCategoryPlaceholder = "???";

        readonly ApplicationSettings settings;

        public ChannelDefinitionRepository(ApplicationSettings settings)
        {
            this.settings = settings;
        }

        public IEnumerable<ChannelDefinitionEntity> GetAll()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<ChannelDefinitionEntity>));
            IEnumerable<ChannelDefinitionEntity> entities;

            using (TextReader reader = new StreamReader(settings.ChannelStorePath))
            {
                entities = (IEnumerable<ChannelDefinitionEntity>)serializer.Deserialize(reader);
            }

            return entities;
        }
    }
}
