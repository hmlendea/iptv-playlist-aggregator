using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using NuciExtensions;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class CacheManager : ICacheManager
    {
        private static char CsvFieldSeparator => ',';
        private static string TimestampFormat => "yyyy-MM-dd_HH-mm-ss";
        private static string PlaylistFileNameFormat => "{0}_playlist_{1:yyyy-MM-dd}.m3u";
        private static string StreamStatusesFileName => "stream-statuses.csv";

        private readonly CacheSettings cacheSettings;

        private readonly ConcurrentDictionary<string, string> normalisedNames;
        private readonly ConcurrentDictionary<string, MediaStreamStatus> streamStatuses;
        private readonly ConcurrentDictionary<string, string> webDownloads;
        private readonly ConcurrentDictionary<int, Playlist> playlists;

        public CacheManager(CacheSettings cacheSettings)
        {
            this.cacheSettings = cacheSettings;

            normalisedNames = new();
            streamStatuses = new();
            webDownloads = new();
            playlists = new();

            PrepareFilesystem();
            LoadStreamStatuses();
        }

        public void SaveCacheToDisk()
            => SaveStreamStatuses();

        public void StoreNormalisedChannelName(string name, string normalisedName)
            => normalisedNames.TryAdd(name, normalisedName);

        public string GetNormalisedChannelName(string name)
            => normalisedNames.TryGetValue(name);

        public void StoreStreamStatus(MediaStreamStatus streamStatus)
            => streamStatuses.TryAdd(streamStatus.Url, streamStatus);

        public MediaStreamStatus GetStreamStatus(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return new()
                {
                    Url = url,
                    State = StreamState.Dead,
                    LastCheckTime = DateTime.UtcNow
                };
            }

            return streamStatuses.TryGetValue(url);
        }

        public void StoreWebDownload(string url, string content)
            => webDownloads.TryAdd(url, content ?? string.Empty);

        public string GetWebDownload(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            string cachedDownload = webDownloads.TryGetValue(url);

            if (cachedDownload is not null)
            {
                return cachedDownload;
            }

            MediaStreamStatus streamStatus = GetStreamStatus(url);

            if (streamStatus is not null && !streamStatus.IsAlive)
            {
                return string.Empty;
            }

            return null;
        }

        public void StorePlaylist(string fileContent, Playlist playlist)
            => playlists.TryAdd(fileContent.GetHashCode(), playlist);

        public Playlist GetPlaylist(string fileContent)
            => playlists.TryGetValue(fileContent.GetHashCode());

        public void StorePlaylistFile(string providerId, DateTime date, string content)
        {
            string fileName = string.Format(PlaylistFileNameFormat, providerId, date);
            string filePath = Path.Combine(cacheSettings.CacheDirectoryPath, fileName);

            File.WriteAllText(filePath, content);
        }

        public string GetPlaylistFile(string providerId, DateTime date)
        {
            string fileName = string.Format(PlaylistFileNameFormat, providerId, date);
            string filePath = Path.Combine(cacheSettings.CacheDirectoryPath, fileName);

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }

            return null;
        }

        private void PrepareFilesystem()
        {
            if (!Directory.Exists(cacheSettings.CacheDirectoryPath))
            {
                Directory.CreateDirectory(cacheSettings.CacheDirectoryPath);
            }
        }

        private void LoadStreamStatuses()
        {
            string filePath = Path.Combine(cacheSettings.CacheDirectoryPath, StreamStatusesFileName);

            if (!File.Exists(filePath))
            {
                return;
            }

            foreach (string line in File.ReadAllLines(filePath))
            {
                string[] fields = line.Split(CsvFieldSeparator);

                MediaStreamStatus streamStatus = new()
                {
                    Url = fields[0],
                    LastCheckTime = DateTime.ParseExact(
                        fields[1],
                        TimestampFormat,
                        CultureInfo.InvariantCulture),
                    State = Enum.Parse<StreamState>(fields[2])
                };

                bool isExpired =
                    (streamStatus.State == StreamState.Alive &&
                        (DateTime.UtcNow - streamStatus.LastCheckTime).TotalSeconds > cacheSettings.StreamAliveStatusCacheTimeout) ||
                    (streamStatus.State == StreamState.Dead &&
                        (DateTime.UtcNow - streamStatus.LastCheckTime).TotalSeconds > cacheSettings.StreamDeadStatusCacheTimeout) ||
                    (streamStatus.State == StreamState.Unauthorised &&
                        (DateTime.UtcNow - streamStatus.LastCheckTime).TotalSeconds > cacheSettings.StreamUnauthorisedStatusCacheTimeout) ||
                    (streamStatus.State == StreamState.NotFound &&
                        (DateTime.UtcNow - streamStatus.LastCheckTime).TotalSeconds > cacheSettings.StreamNotFoundStatusCacheTimeout);

                if (isExpired)
                {
                    continue;
                }

                streamStatuses.TryAdd(streamStatus.Url, streamStatus);
            }
        }

        private void SaveStreamStatuses()
        {
            string filePath = Path.Combine(cacheSettings.CacheDirectoryPath, StreamStatusesFileName);

            List<string> lines = [];

            foreach (MediaStreamStatus streamStatus in streamStatuses.Values)
            {
                string timestamp = streamStatus.LastCheckTime.ToString(
                    TimestampFormat,
                    CultureInfo.InvariantCulture);

                string url = streamStatus.Url;

                int separatorIndex = url.IndexOf(CsvFieldSeparator);

                if (separatorIndex >= 0)
                {
                    url = url[..separatorIndex];
                }

                lines.Add($"{url}{CsvFieldSeparator}{timestamp}{CsvFieldSeparator}{streamStatus.State}");
            }

            File.WriteAllLines(filePath, lines);
        }
    }
}

