using System.Collections.Generic;
using System.IO;
using System.Linq;

using IptvPlaylistFetcher.Core.Configuration;
using IptvPlaylistFetcher.DataAccess.DataObjects;

namespace IptvPlaylistFetcher.DataAccess.Repositories
{
    public sealed class PlaylistProviderRepository : IPlaylistProviderRepository
    {
        const char CsvFieldSeparator = ',';

        readonly ApplicationSettings settings;

        public PlaylistProviderRepository(ApplicationSettings settings)
        {
            this.settings = settings;
        }

        public IEnumerable<PlaylistProviderEntity> GetAll()
        {
            IEnumerable<string> lines = File.ReadAllLines(settings.PlaylistProviderStorePath);
            IList<PlaylistProviderEntity> entities = new List<PlaylistProviderEntity>();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
    
                PlaylistProviderEntity entity = ReadEntity(line);
                entities.Add(entity);
            }

            return entities;
        }

        public static PlaylistProviderEntity ReadEntity(string csvLine)
        {
            string[] fields = csvLine.Split(CsvFieldSeparator);

            PlaylistProviderEntity entity = new PlaylistProviderEntity();
            entity.Id = fields[0];
            entity.Name = fields[1];
            entity.UrlFormat = fields[2];

            return entity;
        }
    }
}