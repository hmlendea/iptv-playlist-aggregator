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
        const string EntryHeaderExtendedInfo = "#EXT-X-STREAM-INF";
        const string EntryHeaderSeparator = ":";
        const string EntryValuesSeparator = ",";
        const string TvGuideChannelNumberTagKey = "tvg-chno";
        const string TvGuideNameTagKey = "tvg-name";
        const string TvGuideIdTagKey = "tvg-id";
        const string TvGuideLogoTagKey = "tvg-logo";
        const string TvGuideCountryTagKey = "tvg-country";
        const string TvGuideGroupTagKey = "group-title";
        const int DefaultEntryRuntime = -1;

        readonly ICacheManager cache;
        readonly ApplicationSettings settings;

        public PlaylistFileBuilder(
            ICacheManager cache,
            ApplicationSettings settings)
        {
            this.cache = cache;
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

            if (!(playlist is null))
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

                    Channel channel = new Channel();
                    channel.Name = lineSplit[lineSplit.Length - 1];

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

            cache.StorePlaylist(content, playlist);

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
    }
}
