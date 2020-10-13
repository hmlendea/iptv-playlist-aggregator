using System;
using System.Security.Cryptography.X509Certificates;

using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service
{
    public interface ICacheManager
    {
        void SaveCacheToDisk();

        void StoreNormalisedChannelName(string name, string normalisedName);
        string GetNormalisedChannelName(string name);

        void StoreSslCertificate(string host, X509Certificate2 certificate);
        X509Certificate2 GetSslCertificate(string host);

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
