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

        static readonly string[] SubstringsToStrip = new string[]
        {
            "(backup)", "(b)", " backup", "(On-Demand)", "[432p]", "[576p]", "[720p]", "[Multi-Audio]", "www.iptvsource.com",
            "(Opt-1)", "(Opt-2)", "(Opt-3)", "(Opt-4)", "(Opt-5)", "(Opt-6)", "(Opt-7)", "(Opt-8)", "(Opt-9)"
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

        public ChannelName(string name, params string[] aliases)
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
            string normalisedName = NormaliseChannelName(name);

            return Aliases.Any(alias => NormaliseChannelName(alias).Equals(normalisedName));
        }

        string NormaliseChannelName(string name)
        {
            return StripChannelName(name).ToUpper().RemoveDiacritics();
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
