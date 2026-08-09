using System;
using System.Collections.Generic;
using System.IO;
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
        public string BuildFile(Playlist playlist)
        {
            int initialCapacity = FileHeader.Length + Environment.NewLine.Length +
                                  (playlist.Channels.Count * BaseChannelEntryCapacity);
            StringBuilder fileBuilder = new(initialCapacity);
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

            using StringReader contentReader = new(content);
            string line = contentReader.ReadLine();
            Channel latestChannel = null;

            while (line is not null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (line.StartsWith(EntryHeader))
                    {
                        string channelName = GetChannelNameFromEntryHeader(line);

                        latestChannel = new()
                        {
                            Name = channelName,
                            PlaylistChannelName = channelName
                        };

                        playlist.Channels.Add(latestChannel);
                    }
                    else if (line.StartsWith(EntryHeaderExtendedInfo))
                    {
                        latestChannel = new();

                        playlist.Channels.Add(latestChannel);
                    }
                    else if (!line.StartsWith('#'))
                    {
                        if (latestChannel is null)
                        {
                            throw new InvalidOperationException("Playlist entry header is missing before a URL line.");
                        }

                        latestChannel.Url = line;
                    }
                }

                line = contentReader.ReadLine();
            }

            cache.StorePlaylist(content, playlist);

            return playlist;
        }

        private static string FileHeader => "#EXTM3U";
        private static string EntryHeader => "#EXTINF";
        private static string EntryHeaderExtendedInfo => "#EXT-X-STREAM-INF";
        private static string EntryHeaderSeparator => ":";
        private static char EntryValuesSeparator => ',';
        private static string TvGuideChannelNumberTagKey => "tvg-chno";
        private static string TvGuideNameTagKey => "tvg-name";
        private static string TvGuideIdTagKey => "tvg-id";
        private static string TvGuideLogoTagKey => "tvg-logo";
        private static string TvGuideCountryTagKey => "tvg-country";
        private static string TvGuideGroupTagKey => "group-title";
        private static string PlaylistIdTagKey => "playlist-id";
        private static string PlaylistChannelNameTagKey => "playlist-channel-name";
        private static int DefaultEntryRuntime => -1;
        private static int BaseChannelEntryCapacity => 96;

        private static string GetChannelNameFromEntryHeader(string entryHeaderLine)
        {
            int channelNameStartIndex = entryHeaderLine.LastIndexOf(EntryValuesSeparator);

            if (channelNameStartIndex < 0)
            {
                return string.Empty;
            }

            return entryHeaderLine[(channelNameStartIndex + 1)..];
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
