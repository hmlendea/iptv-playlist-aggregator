using System;
using System.Linq;

using IptvPlaylistFetcher.Core.Configuration;
using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service
{
    public sealed class PlaylistFileBuilder : IPlaylistFileBuilder
    {
        const string FileHeader = "#EXTM3U";
        const string EntryHeader = "#EXTINF";
        const string EntryHeaderExtendedVersion = "#EXT-X-VERSION";
        const string EntryHeaderExtendedInfo = "#EXT-X-STREAM-INF";
        const string EntryHeaderSeparator = ":";
        const string EntryValuesSeparator = ",";
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
                    file += $" {TvGuideIdTagKey}=\"{channel.Id}\"";

                    if (!string.IsNullOrWhiteSpace(channel.LogoUrl))
                    {
                        file += $" {TvGuideLogoTagKey}=\"{channel.LogoUrl}\"";
                    }

                    if (!string.IsNullOrWhiteSpace(channel.Group))
                    {
                        file += $" {TvGuideGroupTagKey}=\"{channel.Group}\"";
                    }
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

            if (string.IsNullOrWhiteSpace(file))
            {
                return playlist;
            }

            string[] lines = file
                .Replace("\r", "")
                .Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrWhiteSpace(line) ||
                    line.Contains(FileHeader) ||
                    line.Contains(EntryHeaderExtendedVersion)) // TODO: See what to do about this
                {
                    continue;
                }

                if (line.Contains(EntryHeader))
                {
                    Channel channel = new Channel();
                    channel.Name = line.Split(',')[1];

                    playlist.Channels.Add(channel);
                }
                else if (line.Contains(EntryHeaderExtendedInfo))
                {
                    Channel channel = new Channel();
                    // TODO: Where should I take the name from ???

                    playlist.Channels.Add(channel);
                }
                else
                {
                    playlist.Channels.Last().Url = line;
                }
            }

            return playlist;
        }
    }
}
