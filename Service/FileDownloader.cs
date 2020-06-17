using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

using IptvPlaylistAggregator.Configuration;

namespace IptvPlaylistAggregator.Service
{
    public sealed class FileDownloader : IFileDownloader
    {
        readonly IDnsResolver dnsResolver;
        readonly ICacheManager cache;
        readonly ApplicationSettings applicationSettings;

        public FileDownloader(
            IDnsResolver dnsResolver,
            ICacheManager cache,
            ApplicationSettings applicationSettings)
        {
            this.dnsResolver = dnsResolver;
            this.cache = cache;
            this.applicationSettings = applicationSettings;
        }

        public async Task<string> TryDownloadStringAsync(string url)
        {
            string content = cache.GetWebDownload(url);

            if (!(content is null))
            {
                return content;
            }

            try
            {
                return await GetAsync(url);
            }
            catch
            {
                return null;
            }
        }

        async Task<string> GetAsync(string url)
        {
            string resolvedUrl = dnsResolver.ResolveUrl(url);

            if (string.IsNullOrWhiteSpace(resolvedUrl))
            {
                return null;
            }

            string content = null;

            try
            {
                content = await SendGetRequest(resolvedUrl);
            }
            catch (HttpRequestException ex)
            {
                if (ex.InnerException.Message.Contains("SSL"))
                {
                    content = await SendGetRequest(url);
                }
                else
                {
                    throw;
                }
            }

            cache.StoreWebDownload(url, content);

            return content;
        }

        async Task<string> SendGetRequest(string url)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

            HttpClient client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromMilliseconds(3000);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", applicationSettings.UserAgent);

            HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException($"Failed request. Status code {response.StatusCode}");
            }
            
            return await response.Content.ReadAsStringAsync();
        }
    }
}
