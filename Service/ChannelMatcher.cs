using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using NuciExtensions;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class ChannelMatcher : IChannelMatcher
    {
        static readonly string[] SubstringsToStrip = new string[]
        {
            "(backup)", "(b)", " backup", "(On-Demand)", "[432p]", "[576p]", "[720p]", "[Multi-Audio]", "MultiSub", "www.iptvsource.com",
            "(Opt-1)", "(Opt-2)", "(Opt-3)", "(Opt-4)", "(Opt-5)", "(Opt-6)", "(Opt-7)", "(Opt-8)", "(Opt-9)"
        };
        static readonly IDictionary<string, string> SubstringsToReplace = new Dictionary<string, string>
        {
            { "^VIP|RO|", "" },
            { "^ROMANIA", "RO" },
            { "^RUMANIA", "RO" },
            { "^ROM", "RO" },
            { "^RO: *", "" },
            { " RO$", "" },
            { " ROM$", "" },
            { " România$", "" },
            { " Romania$", "" },
            { " S[0-9]$", "" },
            { " [F]*[HMS][DQ]$", "" },
            { " TV$", "" }
        };

        readonly ICacheManager cache;

        public ChannelMatcher(ICacheManager cache)
        {
            this.cache = cache;
        }

        public string NormaliseName(string name)
        {
            string normalisedName = cache.GetNormalisedChannelName(name);

            if (!string.IsNullOrWhiteSpace(normalisedName))
            {
                return normalisedName;
            }

            normalisedName = name.ToUpper().RemoveDiacritics();
            normalisedName = StripChannelName(normalisedName);

            cache.StoreNormalisedChannelName(name, normalisedName);

            return normalisedName;
        }

        public bool DoesMatch(ChannelName name1, string name2)
            => DoChannelNamesMatch(name1.Value, name2) ||
               name1.Aliases.Any(x => DoChannelNamesMatch(x, name2));

        bool DoChannelNamesMatch(string name1, string name2)
            => NormaliseName(name1).Equals(NormaliseName(name2));

        string StripChannelName(string name)
        {
            const string allowedChars =
                "abcdefghijklmnopqrstuvwxyz" +
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "0123456789";
            
            string strippedName = name;

            foreach (string substringToStrip in SubstringsToStrip)
            {
                strippedName = strippedName.Replace(substringToStrip, "", true, CultureInfo.InvariantCulture);
            }

            foreach (string substringToReplace in SubstringsToReplace.Keys)
            {
                strippedName = Regex.Replace(
                    strippedName,
                    substringToReplace,
                    SubstringsToReplace[substringToReplace]);
            }

            string finalString = string.Empty;

            foreach (char c in strippedName)
            {
                if (allowedChars.Contains(c))
                {
                    finalString += c;
                }
            }

            return finalString;
        }
    }
}
