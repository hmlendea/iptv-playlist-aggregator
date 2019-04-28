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
        const char CommentCharacter = '#';

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

                if (!(entity is null))
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }

        public static PlaylistProviderEntity ReadEntity(string csvLine)
        {
            if (!(csvLine is null) &&
                csvLine.Contains(CommentCharacter))
            {
                csvLine = csvLine.Substring(0, csvLine.IndexOf(CommentCharacter));
            }

            if (string.IsNullOrWhiteSpace(csvLine))
            {
                return null;
            }

            string[] fields = csvLine.Split(CsvFieldSeparator);

            PlaylistProviderEntity entity = new PlaylistProviderEntity();
            entity.Id = fields[0];
            entity.Name = fields[1];
            entity.UrlFormat = fields[2];

            return entity;
        }
    }
}