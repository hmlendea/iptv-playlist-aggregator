using System;
using System.Collections.Generic;
using System.Linq;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public sealed class PlaylistFileBuilder : IPlaylistFileBuilder
    {
        const string FileHeader = "#EXTM3U";
        const string EntryHeader = "#EXTINF";
        const string EntryHeaderExtendedVersion = "#EXT-X-VERSION";
        const string EntryHeaderExtendedInfo = "#EXT-X-STREAM-INF";
        const string EntryHeaderSeparator = ":";
        const string EntryValuesSeparator = ",";
        const string TvGuideChannelNumberTagKey = "tvg-chno";
        const string TvGuideNameTagKey = "tvg-name";
        const string TvGuideIdTagKey = "tvg-id";
        const string TvGuideLogoTagKey = "tvg-logo";
        const string TvGuideGroupTagKey = "group-title";
        const int DefaultEntryRuntime = -1;

        readonly ApplicationSettings settings;

        public PlaylistFileBuilder(ApplicationSettings settings)
        {
            this.settings = settings;
        }

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
                
                file +=
                    $"{EntryValuesSeparator}{channel.Name}{Environment.NewLine}" +
                    $"{channel.Url}{Environment.NewLine}";
            }

            return file;
        }

        public Playlist ParseFile(string file)
        {
            Playlist playlist = new Playlist();

            IEnumerable<string> lines = file?
                .Replace("\r", "")
                .Split('\n')
                .Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (string line in lines)
            {
                if (line.StartsWith(EntryHeader))
                {
                    Channel channel = new Channel();
                    channel.Name = line.Split(',')[1];

                    playlist.Channels.Add(channel);
                }
                else if (line.StartsWith(EntryHeaderExtendedInfo))
                {
                    Channel channel = new Channel();
                    // TODO: Where should I take the name from ???

                    playlist.Channels.Add(channel);
                }
                else if (line.StartsWith("#"))
                {
                    continue;
                }
                else
                {
                    playlist.Channels.Last().Url = line;
                }
            }

            return playlist;
        }

        string BuildTvGuideHeaderTags(Channel channel)
        {
            string tvgTags =
                $" {TvGuideChannelNumberTagKey}=\"{channel.Number}\"" +
                $" {TvGuideIdTagKey}=\"{channel.Id}\"" +
                $" {TvGuideNameTagKey}=\"{channel.Name}\"";
                
            if (!string.IsNullOrWhiteSpace(channel.LogoUrl))
            {
                tvgTags += $" {TvGuideLogoTagKey}=\"{channel.LogoUrl}\"";
            }

            if (!string.IsNullOrWhiteSpace(channel.Group))
            {
                tvgTags += $" {TvGuideGroupTagKey}=\"{channel.Group}\"";
            }

            return tvgTags;
        }
    }
}
