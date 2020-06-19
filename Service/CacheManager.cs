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
        const char CsvFieldSeparator = ',';
        const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss";
        const string PlaylistFileNameFormat = "{0}_playlist_{1:yyyy-MM-dd}.m3u";
        const string HostsFileName = "hosts.csv";
        const string StreamStatusesFileName = "stream-statuses.csv";

        readonly CacheSettings cacheSettings;

        readonly ConcurrentDictionary<string, string> normalisedNames;
        readonly ConcurrentDictionary<string, Host> hosts;
        readonly ConcurrentDictionary<string, string> urlResolutions;
        readonly ConcurrentDictionary<string, X509Certificate2> sslCertificates;
        readonly ConcurrentDictionary<string, MediaStreamStatus> streamStatuses;
        readonly ConcurrentDictionary<string, string> webDownloads;
        readonly ConcurrentDictionary<int, Playlist> playlists;

        public CacheManager(CacheSettings cacheSettings)
        {
            this.cacheSettings = cacheSettings;

            normalisedNames = new ConcurrentDictionary<string, string>();
            hosts = new ConcurrentDictionary<string, Host>();
            urlResolutions = new ConcurrentDictionary<string, string>();
            sslCertificates = new ConcurrentDictionary<string, X509Certificate2>();
            streamStatuses = new ConcurrentDictionary<string, MediaStreamStatus>();
            webDownloads = new ConcurrentDictionary<string, string>();
            playlists = new ConcurrentDictionary<int, Playlist>();

            PrepareFilesystem();

            LoadHosts();
            LoadStreamStatuses();
        }

        public void SaveCacheToDisk()
        {
            SaveHosts();
            SaveStreamStatuses();
        }

        public void StoreNormalisedChannelName(string name, string normalisedName)
            => normalisedNames.TryAdd(name, normalisedName);

        public string GetNormalisedChannelName(string name)
            => normalisedNames.TryGetValue(name);

        public void StoreHost(Host host)
        {
            hosts.TryAdd(host.Domain, host);
        }

        public Host GetHost(string domain)
            => hosts.TryGetValue(domain);

        public void StoreUrlResolution(string url, string ip)
        {
            if (ip is null)
            {
                ip = string.Empty;
            }
            
            urlResolutions.TryAdd(url, ip);
        }

        public string GetUrlResolution(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }
            
            return urlResolutions.TryGetValue(url);
        }

        public void StoreSslCertificate(string host, X509Certificate2 certificate)
        {
            if (!string.IsNullOrWhiteSpace(host) && !(certificate is null))
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
        {
            streamStatuses.TryAdd(streamStatus.Url, streamStatus);
        }

        public MediaStreamStatus GetStreamStatus(string url)
        {
            MediaStreamStatus streamStatus = streamStatuses.TryGetValue(url);
            
            string resolvedUrl = urlResolutions.TryGetValue(url);
            
            if (streamStatus is null && !(resolvedUrl is null) && resolvedUrl != url)
            {
                streamStatus = streamStatuses.TryGetValue(resolvedUrl);
            }

            return streamStatus;
        }
        
        public void StoreWebDownload(string url, string content)
            => webDownloads.TryAdd(url, content ?? string.Empty);
        
        public string GetWebDownload(string url)
        {
            string cachedDownload = webDownloads.TryGetValue(url);

            if (!(cachedDownload is null))
            {
                return cachedDownload;
            }
            
            string resolvedUrl = urlResolutions.TryGetValue(url);
            
            if (resolvedUrl is null)
            {
                return null;
            }

            return webDownloads.TryGetValue(resolvedUrl);
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

        void PrepareFilesystem()
        {
            if (!Directory.Exists(cacheSettings.CacheDirectoryPath))
            {
                Directory.CreateDirectory(cacheSettings.CacheDirectoryPath);
            }
        }

        void LoadHosts()
        {
            string filePath = Path.Combine(cacheSettings.CacheDirectoryPath, HostsFileName);

            if (!File.Exists(filePath))
            {
                return;
            }

            List<string> lines = File.ReadAllLines(filePath).ToList();

            foreach (string line in lines)
            {
                string[] fields = line.Split(CsvFieldSeparator);

                Host host = new Host();
                host.Domain = fields[0];
                host.Ip = fields[1];
                host.ResolutionTime = DateTime.ParseExact(fields[2], TimestampFormat, CultureInfo.InvariantCulture);

                if ((DateTime.UtcNow - host.ResolutionTime).TotalSeconds <= cacheSettings.HostCacheTimeout)
                {
                    hosts.TryAdd(host.Domain, host);
                }
            }
        }

        void LoadStreamStatuses()
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

                MediaStreamStatus streamStatus = new MediaStreamStatus();
                streamStatus.Url = fields[0];
                streamStatus.LastCheckTime = DateTime.ParseExact(fields[1], TimestampFormat, CultureInfo.InvariantCulture);
                streamStatus.IsAlive = bool.Parse(fields[2]);

                if ((streamStatus.IsAlive == true && (DateTime.UtcNow - streamStatus.LastCheckTime).TotalSeconds > cacheSettings.StreamAliveStatusCacheTimeout) ||
                    (streamStatus.IsAlive == false && (DateTime.UtcNow - streamStatus.LastCheckTime).TotalSeconds > cacheSettings.StreamDeadStatusCacheTimeout))
                {
                    continue;
                }

                streamStatuses.TryAdd(streamStatus.Url, streamStatus);
            }
        }

        void SaveHosts()
        {
            string filePath = Path.Combine(cacheSettings.CacheDirectoryPath, HostsFileName);

            List<string> lines = new List<string>();

            foreach (Host host in hosts.Values
                .OrderBy(x => x.Domain)
                .ThenBy(x => x.Ip)
                .ThenBy(x => x.ResolutionTime))
            {
                string timestamp = host.ResolutionTime.ToString(
                    TimestampFormat,
                    CultureInfo.InvariantCulture);

                lines.Add($"{host.Domain}{CsvFieldSeparator}{host.Ip}{CsvFieldSeparator}{timestamp}");
            }

            File.WriteAllLines(filePath, lines);
        }

        void SaveStreamStatuses()
        {
            string filePath = Path.Combine(cacheSettings.CacheDirectoryPath, StreamStatusesFileName);

            List<string> lines = new List<string>();

            foreach (MediaStreamStatus streamStatus in streamStatuses.Values)
            {
                string timestamp = streamStatus.LastCheckTime.ToString(
                    TimestampFormat,
                    CultureInfo.InvariantCulture);

                lines.Add($"{streamStatus.Url}{CsvFieldSeparator}{timestamp}{CsvFieldSeparator}{streamStatus.IsAlive}");
            }

            File.WriteAllLines(filePath, lines);
        }
    }
}
