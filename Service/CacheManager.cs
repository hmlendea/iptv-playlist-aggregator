using System;
using System.Collections.Concurrent;
using System.IO;

using NuciExtensions;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class CacheManager : ICacheManager
    {
        const string PlaylistFileNameFOrmat = "{0}_playlist_{1:yyyy-MM-dd}.m3u";

        readonly CacheSettings cacheSettings;

        readonly ConcurrentDictionary<string, string> normalisedNames;
        readonly ConcurrentDictionary<string, string> hostnameResolutions;
        readonly ConcurrentDictionary<string, string> urlResolutions;
        readonly ConcurrentDictionary<string, MediaStreamStatus> streamStatuses;
        readonly ConcurrentDictionary<string, string> webDownloads;
        readonly ConcurrentDictionary<int, Playlist> playlists;

        public CacheManager(CacheSettings cacheSettings)
        {
            this.cacheSettings = cacheSettings;

            normalisedNames = new ConcurrentDictionary<string, string>();
            hostnameResolutions = new ConcurrentDictionary<string, string>();
            urlResolutions = new ConcurrentDictionary<string, string>();
            streamStatuses = new ConcurrentDictionary<string, MediaStreamStatus>();
            webDownloads = new ConcurrentDictionary<string, string>();
            playlists = new ConcurrentDictionary<int, Playlist>();

            PrepareFilesystem();
        }

        public void StoreNormalisedChannelName(string name, string normalisedName)
            => normalisedNames.TryAdd(name, normalisedName);

        public string GetNormalisedChannelName(string name)
            => normalisedNames.TryGetValue(name);

        public void StoreHostnameResolution(string hostname, string ip)
        {
            if (ip is null)
            {
                ip = string.Empty;
            }
            
            hostnameResolutions.TryAdd(hostname, ip);
        }

        public string GetHostnameResolution(string hostname)
            => hostnameResolutions.TryGetValue(hostname);

        public void StoreUrlResolution(string url, string ip)
        {
            if (ip is null)
            {
                ip = string.Empty;
            }
            
            urlResolutions.TryAdd(url, ip);
        }

        public string GetUrlResolution(string url)
            => urlResolutions.TryGetValue(url);

        public void StoreStreamStatus(MediaStreamStatus status)
            => streamStatuses.TryAdd(status.Url, status);

        public MediaStreamStatus GetStreamStatus(string url)
            => streamStatuses.TryGetValue(url);
        
        public void StoreWebDownload(string url, string content)
            => webDownloads.TryAdd(url, content ?? string.Empty);
        
        public string GetWebDownload(string url)
            => webDownloads.TryGetValue(url);
        
        public void StorePlaylist(string fileContent, Playlist playlist)
            => playlists.TryAdd(fileContent.GetHashCode(), playlist);
        
        public Playlist GetPlaylist(string fileContent)
            => playlists.TryGetValue(fileContent.GetHashCode());

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
