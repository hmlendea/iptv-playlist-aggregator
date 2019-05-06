using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

using IptvPlaylistAggregator.Communication;
using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class MediaSourceChecker : IMediaSourceChecker
    {
        const char CsvFieldSeparator = ',';
        const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss";

        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly ApplicationSettings settings;

        readonly IDictionary<string, MediaStreamStatus> statuses;

        public MediaSourceChecker(
            IPlaylistFileBuilder playlistFileBuilder,
            ApplicationSettings settings)
        {
            this.playlistFileBuilder = playlistFileBuilder;
            this.settings = settings;

            statuses = new Dictionary<string, MediaStreamStatus>();

            LoadCache();
        }

        public bool IsSourcePlayable(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (statuses.ContainsKey(url))
            {
                return statuses[url].IsAlive;
            }
            
            bool status;

            if (url.Contains(".m3u") || url.Contains(".m3u8"))
            {
                status = IsPlaylistPlayable(url);
            }
            else
            {
                status = IsStreamPlayable(url);
            }
            
            SaveToCache(url, status);

            return status;
        }

        bool IsPlaylistPlayable(string url)
        {
            Playlist playlist = DownloadPlaylist(url);

            return !Playlist.IsNullOrEmpty(playlist);
        }

        bool IsStreamPlayable(string url)
        {
            try
            {
                UriBuilder uriBuilder = new UriBuilder(url);
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uriBuilder.Uri);
                request.Timeout = 3000;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        Playlist DownloadPlaylist(string url)
        {
            using (FileDownloader client = new FileDownloader(5000))
            {
                try
                {
                    string fileContent = client.DownloadString(url);
                    Playlist playlist = playlistFileBuilder.ParseFile(fileContent);

                    if (!Playlist.IsNullOrEmpty(playlist))
                    {
                        return playlist;
                    }
                }
                catch { }
            }
            
            return null;
        }

        void LoadCache()
        {
            string filePath = Path.Combine(
                settings.CacheDirectoryPath,
                settings.MediaStreamAliveStatusCacheFileName);

            if (!File.Exists(filePath))
            {
                return;
            }

            IList<string> cacheLines = File.ReadAllLines(filePath);

            foreach (string line in cacheLines)
            {
                string[] fields = line.Split(CsvFieldSeparator);

                string url = fields[0];
                bool isAlive = bool.Parse(fields[1]);
                DateTime lastCheckTime = DateTime.ParseExact(
                    fields[2].Replace("\r", "").Replace("\n", ""),
                    TimestampFormat,
                    CultureInfo.InvariantCulture);

                if (DateTime.UtcNow > lastCheckTime.AddMinutes(settings.MediaStreamStatusCacheTimeoutMins))
                {
                    continue;
                }

                MediaStreamStatus status = new MediaStreamStatus();
                status.Url = url;
                status.IsAlive = isAlive;
                status.LastCheckTime = lastCheckTime;

                statuses.Add(url, status);
            }
        }

        void SaveCache()
        {
            string cacheFile = string.Empty;

            foreach (MediaStreamStatus status in statuses.Values)
            {
                string timestamp = status.LastCheckTime.ToString(
                    TimestampFormat,
                    CultureInfo.InvariantCulture);

                cacheFile +=
                    $"{status.Url}{CsvFieldSeparator}" +
                    $"{status.IsAlive}{CsvFieldSeparator}" +
                    $"{timestamp}{Environment.NewLine}";
            }

            string filePath = Path.Combine(
                settings.CacheDirectoryPath,
                settings.MediaStreamAliveStatusCacheFileName);

            File.WriteAllText(filePath, cacheFile);
        }

        void SaveToCache(string url, bool isAlive)
        {
            MediaStreamStatus status = new MediaStreamStatus();
            status.Url = url;
            status.IsAlive = isAlive;
            status.LastCheckTime = DateTime.UtcNow;

            statuses.Add(url, status);
            SaveCache();
        }
    }
}
