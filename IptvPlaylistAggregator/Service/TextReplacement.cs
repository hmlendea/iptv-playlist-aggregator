using System.Text.RegularExpressions;

namespace IptvPlaylistAggregator.Service
{
    /// <summary>
    /// Pairs a compiled regular expression with the replacement applied to a matched value.
    /// </summary>
    internal sealed class TextReplacement
    {
        internal TextReplacement(Regex pattern, string replacement)
        {
            Pattern = pattern;
            Replacement = replacement;
        }

        /// <summary>
        /// Gets the compiled pattern that identifies the text to replace.
        /// </summary>
        internal Regex Pattern { get; }

        /// <summary>
        /// Gets the text that replaces the matched value.
        /// </summary>
        internal string Replacement { get; }
    }
}
