using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using NuciExtensions;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class CacheManager : ICacheManager
    {
        readonly CacheSettings cacheSettings;

        readonly IDictionary<string, string> normalisedNames;
        readonly IDictionary<string, MediaStreamStatus> streamStatuses;

        public CacheManager(CacheSettings cacheSettings)
        {
            this.cacheSettings = cacheSettings;

            normalisedNames = new Dictionary<string, string>();
            streamStatuses = new Dictionary<string, MediaStreamStatus>();
        }

        public void StoreNormalisedChannelName(string name, string normalisedName)
            => normalisedNames.TryAdd(name, normalisedName);

        public string GetNormalisedChannelName(string name)
            => normalisedNames.TryGetValue(name);

        public void StoreStreamStatus(MediaStreamStatus status)
            => streamStatuses.TryAdd(status.Url, status);

        public MediaStreamStatus GetStreamStatus(string url)
            => streamStatuses.TryGetValue(url);
    }
}
