using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using NuciExtensions;

namespace IptvPlaylistAggregator.Service.Models
{
    public sealed class Playlist
    {
        public List<Channel> Channels { get; } = [];

        public bool IsEmpty => EnumerableExt.IsNullOrEmpty(Channels);

        public static bool IsNullOrEmpty([NotNullWhen(false)] Playlist? playlist)
            => playlist is null || playlist.IsEmpty;
    }
}
