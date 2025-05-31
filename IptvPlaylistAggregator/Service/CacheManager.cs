using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

using NuciExtensions;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class CacheManager : ICacheManager
    {
        private const char CsvFieldSeparator = ',';
        private const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss";
        private const string PlaylistFileNameFormat = "{0}_playlist_{1:yyyy-MM-dd}.m3u";
        private const string StreamStatusesFileName = "stream-statuses.csv";

        private readonly CacheSettings cacheSettings;

        private readonly ConcurrentDictionary<string, string> normalisedNames;
        private readonly ConcurrentDictionary<string, X509Certificate2> sslCertificates;
        private readonly ConcurrentDictionary<string, MediaStreamStatus> streamStatuses;
        private readonly ConcurrentDictionary<string, string> webDownloads;
        private readonly ConcurrentDictionary<int, Playlist> playlists;

        public CacheManager(CacheSettings cacheSettings)
        {
            this.cacheSettings = cacheSettings;

            normalisedNames = new ConcurrentDictionary<string, string>();
            sslCertificates = new ConcurrentDictionary<string, X509Certificate2>();
            streamStatuses = new ConcurrentDictionary<string, MediaStreamStatus>();
            webDownloads = new ConcurrentDictionary<string, string>();
            playlists = new ConcurrentDictionary<int, Playlist>();

            PrepareFilesystem();

            LoadStreamStatuses();
        }

        public void SaveCacheToDisk()
            => SaveStreamStatuses();

        public void StoreNormalisedChannelName(string name, string normalisedName)
            => normalisedNames.TryAdd(name, normalisedName);

        public string GetNormalisedChannelName(string name)
            => normalisedNames.TryGetValue(name);

        public void StoreSslCertificate(string host, X509Certificate2 certificate)
        {
            if (!string.IsNullOrWhiteSpace(host) && certificate is not null)
            {
                sslCertificates.TryAdd(host, certificate);
            }
        }

        public X509Certificate2 GetSslCertificate(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return null;
            }

            return sslCertificates.TryGetValue(host);
        }

        public void StoreStreamStatus(MediaStreamStatus streamStatus)
            => streamStatuses.TryAdd(streamStatus.Url, streamStatus);

        public MediaStreamStatus GetStreamStatus(string url)
        {
            MediaStreamStatus streamStatus = streamStatuses.TryGetValue(url);

            if (string.IsNullOrWhiteSpace(url))
            {
                return new MediaStreamStatus()
                {
                    Url = url,
                    State = StreamState.Dead,
                    LastCheckTime = DateTime.UtcNow
                };
            }
            else
            {
                streamStatus ??= streamStatuses.TryGetValue(url);
            }

            return streamStatus;
        }

        public void StoreWebDownload(string url, string content)
            => webDownloads.TryAdd(url, content ?? string.Empty);

        public string GetWebDownload(string url)
        {
            string cachedDownload = webDownloads.TryGetValue(url);

            if (cachedDownload is not null)
            {
                return cachedDownload;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }
            else
            {
                cachedDownload = webDownloads.TryGetValue(url);

                if (cachedDownload is not null)
                {
                    return cachedDownload;
                }
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

            List<string> lines = File.ReadAllLines(filePath).ToList();

            foreach (string line in lines)
            {
                string[] fields = line.Split(CsvFieldSeparator);

                MediaStreamStatus streamStatus = new()
                {
                    Url = fields[0],
                    LastCheckTime = DateTime.ParseExact(fields[1], TimestampFormat, CultureInfo.InvariantCulture),
                    State = (StreamState)Enum.Parse(typeof(StreamState), fields[2])
                };

                if ((streamStatus.State == StreamState.Alive && (DateTime.UtcNow - streamStatus.LastCheckTime).TotalSeconds > cacheSettings.StreamAliveStatusCacheTimeout) ||
                    (streamStatus.State == StreamState.Dead && (DateTime.UtcNow - streamStatus.LastCheckTime).TotalSeconds > cacheSettings.StreamDeadStatusCacheTimeout) ||
                    (streamStatus.State == StreamState.Unauthorised && (DateTime.UtcNow - streamStatus.LastCheckTime).TotalSeconds > cacheSettings.StreamUnauthorisedStatusCacheTimeout) ||
                    (streamStatus.State == StreamState.NotFound && (DateTime.UtcNow - streamStatus.LastCheckTime).TotalSeconds > cacheSettings.StreamNotFoundStatusCacheTimeout))
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

                if (url.Contains(','))
                {
                    url = url.Split(',')[0];
                }

                lines.Add($"{url}{CsvFieldSeparator}{timestamp}{CsvFieldSeparator}{streamStatus.State}");
            }

            File.WriteAllLines(filePath, lines);
        }
    }
}
