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
            "www.iptvsource.com", "iptvsource.com", "backup"
        };

        static readonly IDictionary<string, string> TextReplacements = new Dictionary<string, string>
        {
            { "[\\(\\[]]*([Aa]uto|[Bb]|[Bb]ackup|[Ll]ive [Oo]n [Mm]atches|[Mm]ulti-*[Aa]udio|[Mm]ulti-*[Ss]ub|[Nn]ew!*|[Oo]n-[Dd]emand)[\\)\\]]*", "" },
            { "(.)[ \\.:_\\-\\|\\[\\(\\]\\)\"]+(Ultra|Full|[FU])*[_-]*[HMS][DQ]", "$1" },
            { "4[Kk]\\+", "" },
            
            { "RO\\(L\\) *[\\|\\[\\(\\]\\)\".:-]", "RO:" },

            { "^( *[\\|\\[\\(\\]\\)\".:-]* *([A-Z][A-Z]) *[\\|\\[\\(\\]\\)\".:-] *)+", "$2:" },
            { "^ *([A-Z][A-Z]): *(.*) \\(*\\1\\)*$", "$1: $2" },

            { "Moldavia", "Moldova" },
            { "RUMANIA", "Romania" },

            { "^((?!RO).*) *Moldova$", "MD: $1" },
            { "(.+) +\\(Moldova\\)$", "MD: $1" },
            { "(.+) +\\(Romania\\)$", "RO: $1" },
            { "(.+) +\\(*(RO|MD)\\)*$", "$2: $1" },
            { "^RO *[\\|\\[\\(\\]\\)\".:-] *(.*) *\\(*Romania\\)*$", "RO: $1" },

            { "^[\\|\\[\\(\\]\\)\".:-]* *Romania *[\\|\\[\\(\\]\\)\".:-]", "RO:" },
            
            { " S[0-9]-[0-9]$", "" },
            { " S[0-9]$", "" },
            { "\\(18\\+\\)", "" },
            { "\\(Opt-[0-9]\\)", "" },
            { "[\\[\\(][0-9]*p[\\]\\)]", "" },

            { " HEVC$", "" },
            { " HEVC ", "" },

            { "^ *RO ", "RO: " },
            { " \\(*ROM\\)*$", "" },
            { " *[\\|\\()]*ROM*[\\|\\):]", "RO:" },
            { "^Romania[n]*:", "RO:" },
            { "^ *[\\|]*VIP *([A-Z][A-Z]):", "$1:" },

            { "^ *([A-Z][A-Z]: *)*", "$1" },
            
            { "^(RO: *)*", "" },
        };

        readonly ICacheManager cache;

        public ChannelMatcher(ICacheManager cache)
        {
            this.cache = cache;
        }

        public string NormaliseName(string name, string country)
        {
            string fullName = name;

            if (!string.IsNullOrWhiteSpace(country))
            {
                fullName = $"{country}: {name}";
            }

            string normalisedName = cache.GetNormalisedChannelName(fullName);

            if (!string.IsNullOrWhiteSpace(normalisedName))
            {
                return normalisedName;
            }

            normalisedName = fullName.RemoveDiacritics();
            normalisedName = StripChannelName(normalisedName);
            normalisedName = normalisedName.ToUpper();

            cache.StoreNormalisedChannelName(fullName, normalisedName);

            return normalisedName;
        }

        public bool DoesMatch(ChannelName name1, string name2, string country2)
            => DoChannelNamesMatch(name1.Value, name1.Country, name2, country2) ||
               name1.Aliases.Any(name1alias => DoChannelNamesMatch(name1alias, name1.Country, name2, country2));

        bool DoChannelNamesMatch(string name1, string country1, string name2, string country2)
            => NormaliseName(name1, country1).Equals(NormaliseName(name2, country2));

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
