using System;

using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service
{
    public sealed class PlaylistFileBuilder : IPlaylistFileBuilder
    {
        const string FileHeader = "#EXTM3U";
        const string EntryHeader = "#EXTINF";
        const string EntryHeaderSeparator = ":";
        const string EntryValuesSeparator = ",";
        const int DefaultEntryRuntime = -1;

        public string BuildFile(Playlist playlist)
        {
            string file = FileHeader + Environment.NewLine;

            foreach (Channel channel in playlist.Channels)
            {
                file += 
                    $"{EntryHeader}{EntryHeaderSeparator}" +
                    $"{DefaultEntryRuntime}{EntryValuesSeparator}" +
                    $"{channel.Name}{Environment.NewLine}" +
                    $"{channel.Url}{Environment.NewLine}";
            }

            return file;
        }
    }
}
