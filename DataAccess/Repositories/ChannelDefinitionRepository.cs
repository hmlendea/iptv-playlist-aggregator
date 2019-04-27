using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using IptvPlaylistFetcher.Core.Configuration;
using IptvPlaylistFetcher.DataAccess.DataObjects;

namespace IptvPlaylistFetcher.DataAccess.Repositories
{
    public sealed class ChannelDefinitionRepository : IChannelDefinitionRepository
    {
        const char CsvFieldSeparator = ',';
        const char CsvCollectionSeparator = '|';

        readonly ApplicationSettings settings;

        public ChannelDefinitionRepository(ApplicationSettings settings)
        {
            this.settings = settings;
        }

        public IEnumerable<ChannelDefinitionEntity> GetAll()
        {
            IEnumerable<string> lines = File.ReadAllLines(settings.ChannelStorePath);
            IList<ChannelDefinitionEntity> entities = new List<ChannelDefinitionEntity>();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) ||
                    line.StartsWith('#'))
                {
                    continue;
                }
    
                ChannelDefinitionEntity entity = ReadEntity(line);
                entities.Add(entity);
            }

            return entities;
        }

        ChannelDefinitionEntity ReadEntity(string csvLine)
        {
            if (string.IsNullOrWhiteSpace(csvLine))
            {
                throw new ArgumentNullException(nameof(csvLine));
            }

            string[] fields = csvLine.Split(CsvFieldSeparator);

            if (fields.Length != 3)
            {
                throw new ArgumentException($"Invalid CSV line '{csvLine}'", nameof(csvLine));
            }

            ChannelDefinitionEntity entity = new ChannelDefinitionEntity();
            entity.Id = fields[0];
            entity.Name = fields[1];
            entity.Aliases = fields[2].Split(CsvCollectionSeparator).ToList();

            entity.Aliases.Add(entity.Name);
            entity.Aliases = entity.Aliases.Distinct().ToList();

            return entity;
        }
    }
}
