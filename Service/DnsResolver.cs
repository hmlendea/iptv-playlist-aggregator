using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using NuciExtensions;

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
            string ip = string.Empty;

            lock (this)
            {
                ip = cache.GetHostnameResolution(hostname);

                if (!(ip is null))
                {
                    return ip;
                }

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

            if (!(cachedResolution is null))
            {
                return cachedResolution;
            }

            if (!IsUrlValid(url))
            {
                return null;
            }
            
            Uri uri = new Uri(url);

            string ip = ResolveHostname(uri.Host);

            if (ip is null)
            {
                return null;
            }

            string resolvedUrl = url.ReplaceFirst(uri.Host, ip);

            cache.StoreUrlResolution(url, resolvedUrl);
            return resolvedUrl;
        }

        bool IsUrlValid(string url)
        {
            Uri uri;
            bool isValid = Uri.TryCreate(url, UriKind.Absolute, out uri);

            if (isValid && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return true;
            }

            return false;
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
