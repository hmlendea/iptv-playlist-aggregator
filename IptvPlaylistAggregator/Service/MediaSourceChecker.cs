using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Logging;
using IptvPlaylistAggregator.Service.Models;

using NuciLog.Core;
using NuciWeb.HTTP;

namespace IptvPlaylistAggregator.Service
{
    public sealed class MediaSourceChecker(
        IFileDownloader fileDownloader,
        IPlaylistFileBuilder playlistFileBuilder,
        ICacheManager cache,
        ILogger logger,
        ApplicationSettings applicationSettings) : IMediaSourceChecker
    {
        private const string YouTubeVideoUrlPattern = "^(https?\\:\\/\\/)?(www\\.youtube\\.com|youtu\\.?be)\\/.+$";
        private const string TinyUrlPattern = "^(https?\\:\\/\\/)?((www\\.)?tinyurl\\.com)\\/.+$";
        private const string NonHttpUrlPattern = "^(?!http).*";

        private static readonly string[] BlacklistedSources =
        [
            "http://hls.protv.md/acasatv/acasatv.m3u8"
        ];

        private readonly IFileDownloader fileDownloader = fileDownloader;
        private readonly IPlaylistFileBuilder playlistFileBuilder = playlistFileBuilder;
        private readonly ICacheManager cache = cache;
        private readonly ILogger logger = logger;
        private readonly ApplicationSettings applicationSettings = applicationSettings;

        public async Task<bool> IsSourcePlayableAsync(string url)
        {
            MediaStreamStatus status = cache.GetStreamStatus(url);

            if (status is not null)
            {
                return status.IsAlive;
            }

            logger.Verbose(MyOperation.MediaSourceCheck, OperationStatus.Started, new LogInfo(MyLogInfoKey.Url, url));

            StreamState state;

            if (IsUrlUnsupported(url))
            {
                state = StreamState.Unsupported;
            }
            else if (IsUrlBlacklisted(url))
            {
                state = StreamState.Blacklisted;
            }
            else if (url.Contains(".m3u") || url.Contains(".m3u8"))
            {
                state = await GetPlaylistStateAsync(url);
            }
            else
            {
                state = await GetStreamStateAsync(url);
            }

            if (state == StreamState.Alive)
            {
                logger.Verbose(MyOperation.MediaSourceCheck, OperationStatus.Success, new LogInfo(MyLogInfoKey.Url, url));
            }
            else
            {
                logger.Verbose(MyOperation.MediaSourceCheck, OperationStatus.Failure, new LogInfo(MyLogInfoKey.Url, url), new LogInfo(MyLogInfoKey.StreamState, state));
            }

            SaveToCache(url, state);
            return state == StreamState.Alive;
        }

        private static bool IsUrlUnsupported(string url)
        {
            if (Regex.IsMatch(url, YouTubeVideoUrlPattern) ||
                Regex.IsMatch(url, TinyUrlPattern) ||
                Regex.IsMatch(url, NonHttpUrlPattern))
            {
                return true;
            }

            if (url.EndsWith(".mp4") || url.Contains(".mp4?"))
            {
                return true;
            }

            return false;
        }

        private static bool IsUrlBlacklisted(string url)
        {
            if (BlacklistedSources.Any(x => x.Contains(url)))
            {
                return true;
            }

            return false;
        }

        private async Task<StreamState> GetPlaylistStateAsync(string playlistUrl)
        {
            if (playlistUrl.Contains("googlevideo"))
            {
                return StreamState.Alive;
            }

            StreamState streamState = await GetStreamStateAsync(playlistUrl);

            if (streamState != StreamState.Alive)
            {
                return streamState;
            }

            string fileContent = await fileDownloader.TryDownloadStringAsync(playlistUrl);
            Playlist playlist = playlistFileBuilder.TryParseFile(fileContent);

            if (Playlist.IsNullOrEmpty(playlist))
            {
                return StreamState.Dead;
            }

            foreach (Channel channel in playlist.Channels)
            {
                List<string> channelUrlsToCheck = [];

                if (channel.Url.StartsWith("http"))
                {
                    channelUrlsToCheck.Add(channel.Url);
                }
                else
                {
                    Uri uri = new(playlistUrl);

                    channelUrlsToCheck.Add(Path.GetDirectoryName(playlistUrl).Replace(":/", "://") + "/" + channel.Url);
                    channelUrlsToCheck.Add($"{uri.Scheme}://{uri.Host}/{channel.Url}");
                }

                foreach (string channelUrl in channelUrlsToCheck)
                {
                    bool isPlayable = await IsSourcePlayableAsync(channelUrl);

                    if (isPlayable)
                    {
                        return StreamState.Alive;
                    }
                }
            }

            return StreamState.Dead;
        }

        private async Task<StreamState> GetStreamStateAsync(string url)
        {
            HttpStatusCode statusCode = await GetHttpStatusCode(url);

            if (statusCode == HttpStatusCode.OK)
            {
                return StreamState.Alive;
            }

            if (statusCode == HttpStatusCode.Unauthorized)
            {
                return StreamState.Unauthorised;
            }

            if (statusCode == HttpStatusCode.NotFound)
            {
                return StreamState.NotFound;
            }

            return StreamState.Dead;
        }

        private void SaveToCache(string url, StreamState state)
            => cache.StoreStreamStatus(new()
            {
                Url = url,
                State = state,
                LastCheckTime = DateTime.UtcNow
            });

        private async Task<HttpStatusCode> GetHttpStatusCode(string url)
        {
            HttpStatusCode statusCode = HttpStatusCode.RequestTimeout;
            bool doCacheContent = url.Contains(".m3u") || url.Contains(".m3u8");
            string content = string.Empty;

            try
            {
                HttpWebRequest request = CreateWebRequest(url);

                using HttpWebResponse response = (await request.GetResponseAsync()) as HttpWebResponse;
                statusCode = response.StatusCode;

                if (doCacheContent)
                {
                    using StreamReader reader = new(response.GetResponseStream(), Encoding.UTF8);
                    content = await reader.ReadToEndAsync();
                }
            }
            catch (WebException ex)
            {
                if (ex.Status is WebExceptionStatus.ProtocolError &&
                    ex.Response is HttpWebResponse response)
                {
                    statusCode = response.StatusCode;
                }
            }
            catch { }

            if (doCacheContent)
            {
                cache.StoreWebDownload(url, content);
            }

            return statusCode;
        }

        private HttpWebRequest CreateWebRequest(string url)
        {
            const int timeout = 10000;

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            request.Timeout = timeout;
            request.ContinueTimeout = timeout;
            request.ReadWriteTimeout = timeout;
            request.UserAgent = new UserAgentFetcher().GetUserAgent().Result;

            return request;
        }
    }
}
