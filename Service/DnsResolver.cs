using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace IptvPlaylistAggregator.Service
{
    public sealed class DnsResolver : IDnsResolver
    {
        readonly ICacheManager cache;

        public DnsResolver(ICacheManager cache)
        {
            this.cache = cache;
        }
        
        public string ResolveHostname(string hostname)
        {
            string ip = cache.GetHostnameResolution(hostname);

            if (ip is null)
            {
                if (hostname.Any(x => char.IsLetter(x)))
                {
                    ip = TryGetHostEntry(hostname);
                }
                else
                {
                    ip = hostname;
                }

                cache.StoreHostnameResolution(hostname, ip);
            }

            return ip;
        }
        
        public string ResolveUrl(string url)
        {
            string cachedResolution = cache.GetUrlResolution(url);

            if (!string.IsNullOrWhiteSpace(cachedResolution))
            {
                return cachedResolution;
            }

            Uri uri = new Uri(url);
            string ip = ResolveHostname(uri.Host);

            if (ip is null)
            {
                return null;
            }

            int pos = url.IndexOf(uri.Host);
            string resolvedUrl = url.Substring(0, pos) + ip + url.Substring(pos + uri.Host.Length);
            resolvedUrl = resolvedUrl.Replace("https", "http");

            cache.StoreUrlResolution(url, resolvedUrl);
            return resolvedUrl;
        }

        string TryGetHostEntry(string hostname)
        {
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(hostname);

                if (hostEntry.AddressList.Length != 0)
                {
                    return hostEntry.AddressList
                        .First(addr => addr.AddressFamily == AddressFamily.InterNetwork)
                        .ToString();
                }
            }
            catch { }

            return null;
        }
    }
}
