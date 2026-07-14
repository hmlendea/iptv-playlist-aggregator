using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class PlaylistFileBuilder(
        ICacheManager cache,
        ApplicationSettings settings) : IPlaylistFileBuilder
    {
        private static string FileHeader => "#EXTM3U";
        private static string EntryHeader => "#EXTINF";
        private static string EntryHeaderExtendedInfo => "#EXT-X-STREAM-INF";
        private static string EntryHeaderSeparator => ":";
        private static string EntryValuesSeparator => ",";
        private static string TvGuideChannelNumberTagKey => "tvg-chno";
        private static string TvGuideNameTagKey => "tvg-name";
        private static string TvGuideIdTagKey => "tvg-id";
        private static string TvGuideLogoTagKey => "tvg-logo";
        private static string TvGuideCountryTagKey => "tvg-country";
        private static string TvGuideGroupTagKey => "group-title";
        private static string PlaylistIdTagKey => "playlist-id";
        private static string PlaylistChannelNameTagKey => "playlist-channel-name";
        private static int DefaultEntryRuntime => -1;

        public string BuildFile(Playlist playlist)
        {
            StringBuilder fileBuilder = new();
            fileBuilder.Append(FileHeader).Append(Environment.NewLine);

            foreach (Channel channel in playlist.Channels)
            {
                fileBuilder
                    .Append(EntryHeader)
                    .Append(EntryHeaderSeparator)
                    .Append(DefaultEntryRuntime);

                if (settings.AreTvGuideTagsEnabled)
                {
                    fileBuilder.Append(BuildTvGuideHeaderTags(channel));
                }

                if (settings.ArePlaylistDetailsTagsEnabled)
                {
                    fileBuilder.Append(BuildPlaylistDetailsHeaderTags(channel));
                }

                fileBuilder
                    .Append(EntryValuesSeparator)
                    .Append(channel.Name)
                    .Append(Environment.NewLine)
                    .Append(channel.Url)
                    .Append(Environment.NewLine);
            }

            return fileBuilder.ToString();
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
                .Where(line => !string.IsNullOrWhiteSpace(line));

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
            StringBuilder tvgTagsBuilder = new();
            tvgTagsBuilder
                .Append(' ')
                .Append(TvGuideChannelNumberTagKey)
                .Append("=\"")
                .Append(channel.Number)
                .Append('"')
                .Append(' ')
                .Append(TvGuideIdTagKey)
                .Append("=\"")
                .Append(channel.Id)
                .Append('"')
                .Append(' ')
                .Append(TvGuideNameTagKey)
                .Append("=\"")
                .Append(channel.Name)
                .Append('"');

            if (!string.IsNullOrWhiteSpace(channel.LogoUrl))
            {
                tvgTagsBuilder
                    .Append(' ')
                    .Append(TvGuideLogoTagKey)
                    .Append("=\"")
                    .Append(channel.LogoUrl)
                    .Append('"');
            }

            if (!string.IsNullOrWhiteSpace(channel.Country))
            {
                tvgTagsBuilder
                    .Append(' ')
                    .Append(TvGuideCountryTagKey)
                    .Append("=\"")
                    .Append(channel.Country)
                    .Append('"');
            }

            if (!string.IsNullOrWhiteSpace(channel.Group))
            {
                tvgTagsBuilder
                    .Append(' ')
                    .Append(TvGuideGroupTagKey)
                    .Append("=\"")
                    .Append(channel.Group)
                    .Append('"');
            }

            return tvgTagsBuilder.ToString();
        }

        private static string BuildPlaylistDetailsHeaderTags(Channel channel)
            => $" {PlaylistIdTagKey}=\"{channel.PlaylistId}\"" +
               $" {PlaylistChannelNameTagKey}=\"{channel.PlaylistChannelName}\"";
    }
}

