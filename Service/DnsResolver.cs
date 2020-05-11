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
            if (!IsUrlValid(url))
            {
                return null;
            }
            
            string cachedResolution = cache.GetUrlResolution(url);

            if (!(cachedResolution is null))
            {
                return cachedResolution;
            }

            Uri uri = new Uri(url);

            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return null;
            }

            string ip = ResolveHostname(uri.Host);

            if (ip is null)
            {
                return null;
            }

            string resolvedUrl = url.ReplaceFirst(uri.Host, ip).ReplaceFirst("https", "http");

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
