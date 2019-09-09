using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using NuciExtensions;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class CacheManager : ICacheManager
    {
        const string PlaylistFileNameFOrmat = "{0}_playlist_{1:yyyy-MM-dd}.m3u";

        readonly CacheSettings cacheSettings;

        readonly IDictionary<string, string> normalisedNames;
        readonly IDictionary<string, MediaStreamStatus> streamStatuses;

        public CacheManager(CacheSettings cacheSettings)
        {
            this.cacheSettings = cacheSettings;

            normalisedNames = new Dictionary<string, string>();
            streamStatuses = new Dictionary<string, MediaStreamStatus>();

            PrepareFilesystem();
        }

        public void StoreNormalisedChannelName(string name, string normalisedName)
            => normalisedNames.TryAdd(name, normalisedName);

        public string GetNormalisedChannelName(string name)
            => normalisedNames.TryGetValue(name);

        public void StoreStreamStatus(MediaStreamStatus status)
            => streamStatuses.TryAdd(status.Url, status);

        public MediaStreamStatus GetStreamStatus(string url)
            => streamStatuses.TryGetValue(url);

        public void StorePlaylistFile(string providerId, DateTime date, string content)
        {
            string fileName = string.Format(PlaylistFileNameFOrmat, providerId, date);
            string filePath = Path.Combine(cacheSettings.CacheDirectoryPath, fileName);

            File.WriteAllText(filePath, content);
        }

        public string GetPlaylistFile(string providerId, DateTime date)
        {
            string fileName = string.Format(PlaylistFileNameFOrmat, providerId, date);
            string filePath = Path.Combine(cacheSettings.CacheDirectoryPath, fileName);
            
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }

            return null;
        }

        void PrepareFilesystem()
        {
            if (!Directory.Exists(cacheSettings.CacheDirectoryPath))
            {
                Directory.CreateDirectory(cacheSettings.CacheDirectoryPath);
            }
        }
    }
}
