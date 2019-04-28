using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using IptvPlaylistFetcher.Core.Configuration;
using IptvPlaylistFetcher.DataAccess.Repositories;
using IptvPlaylistFetcher.Service.Mapping;
using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service
{
    public sealed class PlaylistFetcher : IPlaylistFetcher
    {
        const string CacheFileNameFormat = "{0}_playlist_{1:yyyy-MM-dd}.m3u";

        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly ApplicationSettings settings;

        public PlaylistFetcher(
            IPlaylistFileBuilder playlistFileBuilder,
            ApplicationSettings settings)
        {
            this.playlistFileBuilder = playlistFileBuilder;
            this.settings = settings;
        }

        public IEnumerable<Playlist> FetchProviderPlaylists(IEnumerable<PlaylistProvider> providers)
        {
            IList<Playlist> playlists = new List<Playlist>();

            foreach (PlaylistProvider provider in providers)
            {
                Playlist playlist = FetchProviderPlaylist(provider);

                if (!(playlist is null))
                {
                    playlists.Add(playlist);
                }
            }

            return playlists;
        }

        public Playlist FetchProviderPlaylist(PlaylistProvider provider)
        {
            Playlist playlist = null;

            for (int i = 0; i < settings.DaysToCheck; i++)
            {
                DateTime date = DateTime.Now.AddDays(-i);

                playlist = LoadPlaylistFromCache(provider, date);
                
                if (playlist is null)
                {
                    playlist = DownloadPlaylist(provider, date);
                }
                
                if (!(playlist is null))
                {
                    break;
                }
            }

            if (!(playlist is null) &&
                !string.IsNullOrWhiteSpace(provider.ChannelNameOverride))
            {
                foreach (Channel channel in playlist.Channels)
                {
                    channel.Name = provider.ChannelNameOverride;
                }
            }
            
            return playlist;
        }

        void SaveProviderPlaylistToCache(string providerId, DateTime date, string playlist)
        {
            if (!Directory.Exists(settings.CacheDirectoryPath))
            {
                Directory.CreateDirectory(settings.CacheDirectoryPath);
            }

            string filePath = Path.Combine(
                settings.CacheDirectoryPath,
                string.Format(CacheFileNameFormat, providerId, date));

            File.WriteAllText(filePath, playlist);
        }

        Playlist LoadPlaylistFromCache(PlaylistProvider provider, DateTime date)
        {
            string filePath = Path.Combine(
                settings.CacheDirectoryPath,
                string.Format(CacheFileNameFormat, provider.Id, date));
            
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                
                return playlistFileBuilder.ParseFile(fileContent);
            }

            return null;
        }

        Playlist DownloadPlaylist(PlaylistProvider provider, DateTime date)
        {
            string url = string.Format(provider.UrlFormat, date);

            Console.Write($"GET '{url}' ... ");

            using (WebClient client = new WebClient())
            {
                try
                {
                    string fileContent = client.DownloadString(url);
                    Playlist playlist = playlistFileBuilder.ParseFile(fileContent);

                    if (!Playlist.IsNullOrEmpty(playlist))
                    {
                        SaveProviderPlaylistToCache(provider.Id, date, fileContent);
                        Console.WriteLine("SUCCESS");

                        return playlist;
                    }
                }
                catch { }
            }

            Console.WriteLine("FAIL");
            return null;
        }
    }
}
