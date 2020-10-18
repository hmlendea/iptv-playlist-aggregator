using System;
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

namespace IptvPlaylistAggregator.Service
{
    public sealed class MediaSourceChecker : IMediaSourceChecker
    {
        const string YouTubeVideoUrlPattern = "^(https?\\:\\/\\/)?(www\\.youtube\\.com|youtu\\.?be)\\/.+$";
        const string NonHttpUrlPattern = "^(?!http).*";

        readonly IFileDownloader fileDownloader;
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly ICacheManager cache;
        readonly ILogger logger;
        readonly ApplicationSettings applicationSettings;

        public MediaSourceChecker(
            IFileDownloader fileDownloader,
            IPlaylistFileBuilder playlistFileBuilder,
            ICacheManager cache,
            ILogger logger,
            ApplicationSettings applicationSettings)
        {
            this.fileDownloader = fileDownloader;
            this.playlistFileBuilder = playlistFileBuilder;
            this.cache = cache;
            this.logger = logger;
            this.applicationSettings = applicationSettings;
        }

        public async Task<bool> IsSourcePlayableAsync(string url)
        {
            MediaStreamStatus status = cache.GetStreamStatus(url);

            if (!(status is null))
            {
                return status.IsAlive;
            }

            logger.Verbose(MyOperation.MediaSourceCheck, OperationStatus.Started, new LogInfo(MyLogInfoKey.Url, url));

            StreamState state;

            if (IsUrlUnsupported(url))
            {
                state = StreamState.Unsupported;
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
        
        bool IsUrlUnsupported(string url)
        {
            if (Regex.IsMatch(url, YouTubeVideoUrlPattern) ||
                Regex.IsMatch(url, NonHttpUrlPattern))
            {
                return true;
            }

            return false;
        }

        async Task<StreamState> GetPlaylistStateAsync(string url)
        {
            StreamState streamState = await GetStreamStateAsync(url);
            
            if (streamState != StreamState.Alive)
            {
                return streamState;
            }

            string fileContent = await fileDownloader.TryDownloadStringAsync(url);
            Playlist playlist = playlistFileBuilder.TryParseFile(fileContent);

            if (Playlist.IsNullOrEmpty(playlist))
            {
                return StreamState.Dead;
            }

            int aliveChannelsCount = 0;

            foreach (Channel channel in playlist.Channels)
            {
                string channelUrl = channel.Url;

                if (!channelUrl.StartsWith("http"))
                {
                    // TODO: Replace this with something proper
                    channelUrl = Path.GetDirectoryName(url).Replace(":/", "://") + "/" + channelUrl;
                }

                bool isPlayable = await IsSourcePlayableAsync(channelUrl);

                if (isPlayable)
                {
                    aliveChannelsCount += 1;
                }
            }

            if (aliveChannelsCount == 0)
            {
                return StreamState.Dead;
            }

            return StreamState.Alive;
        }

        async Task<StreamState> GetStreamStateAsync(string url)
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

        void SaveToCache(string url, StreamState state)
        {
            MediaStreamStatus status = new MediaStreamStatus();
            status.Url = url;
            status.State = state;
            status.LastCheckTime = DateTime.UtcNow;

            cache.StoreStreamStatus(status);
        }

        async Task<HttpStatusCode> GetHttpStatusCode(string url)
        {
            HttpStatusCode statusCode = HttpStatusCode.RequestTimeout;
            bool doCacheContent = url.Contains(".m3u") || url.Contains(".m3u8");
            string content = string.Empty;

            try
            {
                HttpWebRequest request = CreateWebRequest(url);

                using (HttpWebResponse response = (await request.GetResponseAsync()) as HttpWebResponse)
                {
                    statusCode = response.StatusCode;

                    if (doCacheContent)
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        {
                            content = await reader.ReadToEndAsync();
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse response = ex.Response as HttpWebResponse;

                    if (response != null)
                    {
                        statusCode = response.StatusCode;
                    }
                }
            }
            catch { }

            if (doCacheContent)
            {
                cache.StoreWebDownload(url, content);
            }

            return statusCode;
        }

        HttpWebRequest CreateWebRequest(string url)
        {
            const int timeout = 9000;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeout;
            request.ContinueTimeout = timeout;
            request.ReadWriteTimeout = timeout;
            request.UserAgent = applicationSettings.UserAgent;

            return request;
        }
    }
}
