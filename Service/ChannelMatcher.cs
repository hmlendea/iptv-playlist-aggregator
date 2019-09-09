using System.Globalization;
using System.Linq;

using NuciExtensions;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class ChannelMatcher : IChannelMatcher
    {
        static readonly string[] SubstringsToStrip = new string[]
        {
            "(backup)", "(b)", " backup", "(On-Demand)", "[432p]", "[576p]", "[720p]", "[Multi-Audio]", "www.iptvsource.com",
            "(Opt-1)", "(Opt-2)", "(Opt-3)", "(Opt-4)", "(Opt-5)", "(Opt-6)", "(Opt-7)", "(Opt-8)", "(Opt-9)"
        };

        readonly ICacheManager cache;

        public ChannelMatcher(ICacheManager cache)
        {
            this.cache = cache;
        }

        public bool DoChannelNamesMatch(ChannelName name1, string name2)
            => name1.Aliases.Any(x => DoChannelNamesMatch(x, name2));

        bool DoChannelNamesMatch(string name1, string name2)
            => NormaliseChannelName(name1).Equals(NormaliseChannelName(name2));
        
        string NormaliseChannelName(string name)
        {
            string normalisedName = cache.GetNormalisedChannelName(name);

            if (!string.IsNullOrWhiteSpace(normalisedName))
            {
                return normalisedName;
            }

            normalisedName = StripChannelName(name)
                .ToUpper()
                .RemoveDiacritics();

            cache.StoreNormalisedChannelName(name, normalisedName);

            return normalisedName;
        }

        string StripChannelName(string name)
        {
            string strippedName = name;

            foreach (string substringToStrip in SubstringsToStrip)
            {
                strippedName = strippedName.Replace(substringToStrip, "", true, CultureInfo.InvariantCulture);
            }

            string finalString = string.Empty;

            foreach (char c in strippedName)
            {
                if (char.IsLetterOrDigit(c))
                {
                    finalString += c;
                }
            }

            return finalString;
        }
    }
}
