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
            "(Auto)", "(backup)", "(b)", " backup", "(On-Demand)", "(New!)", "(Live On Matches)", "[Multi-Audio]", "[Multi-Sub]", "MultiSub",
            "www.iptvsource.com", "iptvsource.com"
        };

        static readonly IDictionary<string, string> TextReplacements = new Dictionary<string, string>
        {
            { "[\\ _-](Ultra|[FU])*_*[HMS][DQ]", "" },

            { "^[\\|\":-]* *([A-Z][A-Z]) *[\\|\":-] *", "$1:" },
            { "^([A-Z][A-Z]): *(.*) \\(*\\1\\)*$", "$1: $2" },

            { "Moldavia", "Moldova" },
            { "RUMANIA", "Romania" },
            { "(.*) +RO$", "RO: $1" },

            { "^((?!RO).*) *Moldova$", "MD: $1" },
            { "(.*) *\\(Moldova\\)$", "MD: $1" },
            { "(.*) *\\(Romania\\)$", "RO: $1" },

            { "^[\\|\":-]* *Romania *[\\|\":-]", "RO:" },
            
            { " S[0-9]-[0-9]$", "" },
            { " S[0-9]$", "" },
            { "\\(18\\+\\)", "" },
            { "\\(Opt-[0-9]\\)", "" },
            { "[\\[\\(][0-9]*p[\\]\\)]", "" },

            { " HEVC$", "" },
            { " HEVC ", "" },

            { " \\(*ROM\\)*$", "" },
            { "[\\|]*ROM*[\\|:]", "RO:" },
            { "^[\\|]*VIP([A-Z][A-Z]):", "$1:" },

            { "^([A-Z][A-Z]: *)*", "$1" },
            
            { "^(RO: *)*", "" },
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

            normalisedName = name.RemoveDiacritics();
            normalisedName = StripChannelName(normalisedName);
            normalisedName = normalisedName.ToUpper();

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

            foreach (string pattern in TextReplacements.Keys)
            {
                strippedName = Regex.Replace(
                    strippedName,
                    pattern,
                    TextReplacements[pattern],
                    RegexOptions.IgnoreCase);
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
