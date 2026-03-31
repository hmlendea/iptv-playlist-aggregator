using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using NuciExtensions;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class ChannelMatcher(ICacheManager cache) : IChannelMatcher
    {
        private static readonly string[] SubstringsToStrip = [ "www.iptvsource.com", "iptvsource.com", "backup" ];

        private static readonly RegexOptions RegexReplacementOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled;

        private static readonly IDictionary<string, string> TextReplacements = new Dictionary<string, string>
        {
            { "^\\s+", "" },
            { "\\s+$", "" },
            { "&amp;", "&" },

            { "[\\(\\[]]*([Aa]uto|[Bb]|[Bb]ackup|[Ll]ive [Oo]n [Mm]atches|[Mm]atch[ -]*[Tt]ime|[Mm]ulti-*[Aa]udio|[Mm]ulti-*[Ss]ub|[Nn]ew!*|[Oo]n-[Dd]emand)[\\)\\]]*", "" },
            { "(.)[ \\.:_\\-\\|\\[\\(\\]\\)\"]+(Ultra|Full|[FU])*[_-]*[HMS][DQ]", "$1" },
            { "4[Kk]\\+", "" },
            { "\\((270|406|480|540|576|720|1080|2160)p\\)", "" },

            { "\\[Not 24/7\\]", "" },
            { "\\[Geo-blocked\\]", "" },

            { "^(.+)\\s+VIP\\s+([A-Z][A-Z])\\s*$", "$2: $1" },

            { "RO\\(L\\) *[\\|\\[\\(\\]\\)\".:-]", "RO:" },

            { "^([\\|\\[\\(\\]\\)\".:-]* *([A-Z][A-Z]) *[\\|\\[\\(\\]\\)\".:-] *)+", "$2:" },
            { "^([A-Z][A-Z]): *(.*) \\(*\\1\\)*$", "$1: $2" },

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

            { "^RO ", "RO: " },
            { " \\(*ROM\\)*$", "" },
            { " *[\\|\\()]*ROM*[\\|\\):]", "RO:" },
            { "^Romania[n]*:", "RO:" },
            { "^[\\|]*VIP *([A-Z][A-Z]):", "$1:" },

            { "^([A-Z][A-Z]: *)*", "$1" },

            { "^(RO: *)*", "" },
        };

        private static readonly (Regex Pattern, string Replacement)[] CompiledTextReplacements =
            TextReplacements
                .Select(entry => (new Regex(entry.Key, RegexReplacementOptions), entry.Value))
                .ToArray();

        private readonly ICacheManager cache = cache;

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

        private bool DoChannelNamesMatch(string name1, string country1, string name2, string country2)
            => name1.Equals(name2) || NormaliseName(name1, country1).Equals(NormaliseName(name2, country2));

        private static string StripChannelName(string name)
        {
            string strippedName = name;

            foreach (string substringToStrip in SubstringsToStrip)
            {
                strippedName = strippedName.Replace(substringToStrip, "", true, CultureInfo.InvariantCulture);
            }

            foreach ((Regex pattern, string replacement) in CompiledTextReplacements)
            {
                strippedName = pattern.Replace(strippedName, replacement);
            }

            StringBuilder finalString = new(strippedName.Length);

            foreach (char c in strippedName)
            {
                if (IsAsciiLetterOrDigit(c))
                {
                    finalString.Append(c);
                }
            }

            return finalString.ToString();
        }

        private static bool IsAsciiLetterOrDigit(char c)
            => (c >= 'a' && c <= 'z') ||
               (c >= 'A' && c <= 'Z') ||
               (c >= '0' && c <= '9');
    }
}
