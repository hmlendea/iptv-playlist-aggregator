using System;
using System.Net;

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
            string ip = cache.GetDnsEntry(hostname);

            if (ip is null)
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(hostname);
                ip = string.Empty;

                if (hostEntry.AddressList.Length != 0)
                {
                    ip = hostEntry.AddressList[0].ToString();
                }

                cache.StoreDnsEntry(hostname, ip);
            }

            return ip;
        }
        
        public string ResolveUrl(string url)
        {
            Uri uri = new Uri(url);
            string ip = ResolveHostname(uri.Host);

            if (ip is null)
            {
                return null;
            }

            int pos = url.IndexOf(uri.Host);
            return url.Substring(0, pos) + ip + url.Substring(pos + uri.Host.Length);
        }
    }
}
