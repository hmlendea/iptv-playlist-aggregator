using System;
using System.Collections.Generic;
using System.Linq;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class PlaylistFileBuilder(
        ICacheManager cache,
        ApplicationSettings settings) : IPlaylistFileBuilder
    {
        private const string FileHeader = "#EXTM3U";
        private const string EntryHeader = "#EXTINF";
        private const string EntryHeaderExtendedInfo = "#EXT-X-STREAM-INF";
        private const string EntryHeaderSeparator = ":";
        private const string EntryValuesSeparator = ",";
        private const string TvGuideChannelNumberTagKey = "tvg-chno";
        private const string TvGuideNameTagKey = "tvg-name";
        private const string TvGuideIdTagKey = "tvg-id";
        private const string TvGuideLogoTagKey = "tvg-logo";
        private const string TvGuideCountryTagKey = "tvg-country";
        private const string TvGuideGroupTagKey = "group-title";
        private const string PlaylistIdTagKey = "playlist-id";
        private const string PlaylistChannelNameTagKey = "playlist-channel-name";
        private const int DefaultEntryRuntime = -1;

        private readonly ICacheManager cache = cache;
        private readonly ApplicationSettings settings = settings;

        public string BuildFile(Playlist playlist)
        {
            string file = FileHeader + Environment.NewLine;

            foreach (Channel channel in playlist.Channels)
            {
                file +=
                    $"{EntryHeader}{EntryHeaderSeparator}" +
                    $"{DefaultEntryRuntime}";

                if (settings.AreTvGuideTagsEnabled)
                {
                    file += BuildTvGuideHeaderTags(channel);
                }

                if (settings.ArePlaylistDetailsTagsEnabled)
                {
                    file += BuildPlaylistDetailsHeaderTags(channel);
                }

                file +=
                    $"{EntryValuesSeparator}{channel.Name}{Environment.NewLine}" +
                    $"{channel.Url}{Environment.NewLine}";
            }

            return file;
        }

        public Playlist TryParseFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return null;
            }

            try
            {
                return ParseFile(file);
            }
            catch
            {
                return null;
            }
        }

        public Playlist ParseFile(string content)
        {
            Playlist playlist = cache.GetPlaylist(content);

            if (playlist is not null)
            {
                return playlist;
            }

            playlist = new Playlist();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentNullException(nameof(content));
            }

            IEnumerable<string> lines = content
                .Replace("\r", "")
                .Split('\n')
                .Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (string line in lines)
            {
                if (line.StartsWith(EntryHeader))
                {
                    string[] lineSplit = line.Split(',');

                    Channel channel = new()
                    {
                        Name = lineSplit[lineSplit.Length - 1]
                    };
                    channel.PlaylistChannelName = channel.Name;

                    playlist.Channels.Add(channel);
                }
                else if (line.StartsWith(EntryHeaderExtendedInfo))
                {
                    Channel channel = new();
                    // TODO: Where should I take the name from ???

                    playlist.Channels.Add(channel);
                }
                else if (line.StartsWith('#'))
                {
                    continue;
                }
                else
                {
                    playlist.Channels.Last().Url = line;
                }
            }

            cache.StorePlaylist(content, playlist);

            return playlist;
        }

        private static string BuildTvGuideHeaderTags(Channel channel)
        {
            string tvgTags =
                $" {TvGuideChannelNumberTagKey}=\"{channel.Number}\"" +
                $" {TvGuideIdTagKey}=\"{channel.Id}\"" +
                $" {TvGuideNameTagKey}=\"{channel.Name}\"";

            if (!string.IsNullOrWhiteSpace(channel.LogoUrl))
            {
                tvgTags += $" {TvGuideLogoTagKey}=\"{channel.LogoUrl}\"";
            }

            if (!string.IsNullOrWhiteSpace(channel.Country))
            {
                tvgTags += $" {TvGuideCountryTagKey}=\"{channel.Country}\"";
            }

            if (!string.IsNullOrWhiteSpace(channel.Group))
            {
                tvgTags += $" {TvGuideGroupTagKey}=\"{channel.Group}\"";
            }

            return tvgTags;
        }

        private static string BuildPlaylistDetailsHeaderTags(Channel channel)
            => $" {PlaylistIdTagKey}=\"{channel.PlaylistId}\"" +
               $" {PlaylistChannelNameTagKey}=\"{channel.PlaylistChannelName}\"";
    }
}
