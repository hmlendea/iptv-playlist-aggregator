using System;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public interface ICacheManager
    {
        void StoreNormalisedChannelName(string name, string normalisedName);
        string GetNormalisedChannelName(string name);

        void StoreHostnameResolution(string hostname, string ip);
        string GetHostnameResolution(string hostname);

        void StoreUrlResolution(string url, string ip);
        string GetUrlResolution(string url);

        void StoreStreamStatus(MediaStreamStatus status);
        MediaStreamStatus GetStreamStatus(string url);

        void StoreWebDownload(string url, string content);
        string GetWebDownload(string url);

        void StorePlaylist(string fileContent, Playlist playlist);
        Playlist GetPlaylist(string fileContent);

        void StorePlaylistFile(string name, DateTime date, string content);
        string GetPlaylistFile(string name, DateTime date);
    }
}
