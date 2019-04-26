using System.Collections.Generic;
using System.IO;
using System.Linq;

using IptvPlaylistFetcher.Core.Configuration;
using IptvPlaylistFetcher.DataAccess.DataObjects;

namespace IptvPlaylistFetcher.DataAccess.Repositories
{
    public sealed class ChannelRepository : IChannelRepository
    {
        const char CsvFieldSeparator = ',';
        const char CsvCollectionSeparator = '|';

        readonly ApplicationSettings settings;

        public ChannelRepository(ApplicationSettings settings)
        {
            this.settings = settings;
        }

        public IEnumerable<ChannelEntity> GetAll()
        {
            IEnumerable<string> lines = File.ReadAllLines(settings.ChannelStorePath);
            IList<ChannelEntity> entities = new List<ChannelEntity>();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
    
                ChannelEntity entity = ReadEntity(line);
                entities.Add(entity);
            }

            return entities;
        }

        public static ChannelEntity ReadEntity(string csvLine)
        {
            string[] fields = csvLine.Split(CsvFieldSeparator);

            ChannelEntity entity = new ChannelEntity();
            entity.Id = fields[0];
            entity.Name = fields[1];
            entity.Aliases = fields[2].Split(CsvCollectionSeparator).ToList();

            entity.Aliases.Add(entity.Name);
            entity.Aliases = entity.Aliases.Distinct().ToList();

            return entity;
        }
    }
}
