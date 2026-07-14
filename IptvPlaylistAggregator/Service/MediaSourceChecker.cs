using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NuciLog.Core;

using NuciWeb.HTTP;

using IptvPlaylistAggregator.Logging;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class MediaSourceChecker(
        IFileDownloader fileDownloader,
        IPlaylistFileBuilder playlistFileBuilder,
        ICacheManager cache,
        ILogger logger) : IMediaSourceChecker
    {
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
                logger.Verbose(
                    MyOperation.MediaSourceCheck,
                    OperationStatus.Failure,
                    new LogInfo(MyLogInfoKey.Url, url),
                    new LogInfo(MyLogInfoKey.StreamState, state));
            }

            SaveToCache(url, state);

            return state == StreamState.Alive;
        }

        private static string YouTubeVideoUrlPattern => "^(https?\\:\\/\\/)?(www\\.youtube\\.com|youtu\\.?be)\\/.+$";
        private static string TinyUrlPattern => "^(https?\\:\\/\\/)?((www\\.)?tinyurl\\.com)\\/.+$";
        private static string NonHttpUrlPattern => "^(?!http).*";

        private static IEnumerable<string> BlacklistedSources => [ "http://hls.protv.md/acasatv/acasatv.m3u8" ];

        private readonly HttpClient httpClient = HttpClientCreator.Create();

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
            => BlacklistedSources.Any(blacklistedSource => blacklistedSource.Contains(url));

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
            bool isPlaylistUrl = url.Contains(".m3u") || url.Contains(".m3u8");
            string content = string.Empty;

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(url);
                statusCode = response.StatusCode;

                if (isPlaylistUrl)
                {
                    content = await response.Content.ReadAsStringAsync();
                }
            }
            catch (WebException webException)
            {
                if (webException.Status is WebExceptionStatus.ProtocolError &&
                    webException.Response is HttpWebResponse httpWebResponse)
                {
                    statusCode = httpWebResponse.StatusCode;
                }
            }
            catch { }

            if (isPlaylistUrl)
            {
                cache.StoreWebDownload(url, content);
            }

            return statusCode;
        }
    }
}
