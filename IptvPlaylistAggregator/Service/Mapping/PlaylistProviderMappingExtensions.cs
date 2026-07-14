using System.Collections.Generic;
using System.Linq;

using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service.Mapping
{
    internal static class PlaylistProviderMappingExtensions
    {
        internal static PlaylistProvider ToDomainModel(this PlaylistProviderDataObject dataObject) => new()
        {
            Id = dataObject.Id,
            IsEnabled = dataObject.IsEnabled,
            Priority = dataObject.Priority,
            IsCachingEnabled = dataObject.IsCachingEnabled,
            Name = dataObject.Name,
            UrlFormat = dataObject.UrlFormat,
            Country = dataObject.Country,
            ChannelNameOverride = dataObject.ChannelNameOverride
        };

        internal static PlaylistProviderDataObject ToDataObject(
            this PlaylistProvider playlistProvider) => new()
        {
            Id = playlistProvider.Id,
            IsEnabled = playlistProvider.IsEnabled,
            Priority = playlistProvider.Priority,
            IsCachingEnabled = playlistProvider.IsCachingEnabled,
            Name = playlistProvider.Name,
            UrlFormat = playlistProvider.UrlFormat,
            Country = playlistProvider.Country,
            ChannelNameOverride = playlistProvider.ChannelNameOverride
        };

        internal static IEnumerable<PlaylistProvider> ToDomainModels(
            this IEnumerable<PlaylistProviderDataObject> dataObjects)
            => dataObjects.Select(dataObject => dataObject.ToDomainModel());

        internal static IEnumerable<PlaylistProviderDataObject> ToDataObjects(
            this IEnumerable<PlaylistProvider> playlistProviders)
            => playlistProviders.Select(playlistProvider => playlistProvider.ToDataObject());
    }
}
