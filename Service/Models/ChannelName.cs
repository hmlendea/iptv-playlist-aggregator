using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using NuciExtensions;

namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class ChannelName : IEquatable<ChannelName>
    {
        public string Value { get; set; }

        public IEnumerable<string> Aliases { get; set; }

        static readonly IEnumerable<string> SubstringsToStrip = new List<string>
        {
            "(backup)", "(b)", " backup", "(On-Demand)", "(Opt-1)", "[432p]", "[576p]", "[720p]"
        };

        public ChannelName(string name)
            : this(name, new List<string>())
        {
            
        }

        public ChannelName(string name, IEnumerable<string> aliases)
        {
            Value = name;
            Aliases = aliases;
        }

        public bool Equals(ChannelName other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                Equals(Value, other.Value) ||
                Aliases.Any(other.Equals) ||
                other.Aliases.Any(Equals);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ChannelName)obj);
        }

        public bool Equals(string name)
        {
            return Aliases.Any(alias =>
                NormaliseChannelName(name).Equals(NormaliseChannelName(alias)));
        }

        string NormaliseChannelName(string name)
        {
            string normalisedName = string.Empty;
            string strippedName = name;

            foreach (string substringToStrip in SubstringsToStrip)
            {
                strippedName = strippedName.Replace(substringToStrip, "", true, CultureInfo.CurrentCulture);
            }

            foreach (char c in name.Where(char.IsLetterOrDigit))
            {
                normalisedName += char.ToUpper(c);
            }

            return normalisedName.RemoveDiacritics();
        }

        public static bool operator ==(ChannelName source, ChannelName other)
            => source.Equals(other);

        public static bool operator !=(ChannelName source, ChannelName other)
            => !source.Equals(other);

        public static bool operator ==(ChannelName source, string other)
            => source.Equals(other);

        public static bool operator !=(ChannelName source, string other)
            => !source.Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Value.GetHashCode() * 528;

                foreach (string alias in Aliases)
                {
                    hashCode = hashCode ^ alias.GetHashCode();
                }

                return hashCode;
            }
        }
    }
}
