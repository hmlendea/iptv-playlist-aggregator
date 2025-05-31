using System.Collections.Generic;
using System.Linq;

using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service.Mapping
{
    internal static class PlaylistProviderMapping
    {
        internal static PlaylistProvider ToServiceModel(this PlaylistProviderEntity dataObject) => new()
        {
            Id = dataObject.Id,
            IsEnabled = dataObject.IsEnabled,
            Priority = dataObject.Priority,
            AllowCaching = dataObject.AllowCaching,
            Name = dataObject.Name,
            UrlFormat = dataObject.UrlFormat,
            Country = dataObject.Country,
            ChannelNameOverride = dataObject.ChannelNameOverride
        };

        internal static PlaylistProviderEntity ToDataObject(this PlaylistProvider serviceModel) => new()
        {
            Id = serviceModel.Id,
            IsEnabled = serviceModel.IsEnabled,
            Priority = serviceModel.Priority,
            AllowCaching = serviceModel.AllowCaching,
            Name = serviceModel.Name,
            UrlFormat = serviceModel.UrlFormat,
            Country = serviceModel.Country,
            ChannelNameOverride = serviceModel.ChannelNameOverride
        };

        internal static IEnumerable<PlaylistProvider> ToServiceModels(this IEnumerable<PlaylistProviderEntity> dataObjects)
            => dataObjects.Select(dataObject => dataObject.ToServiceModel());

        internal static IEnumerable<PlaylistProviderEntity> ToEntities(this IEnumerable<PlaylistProvider> serviceModels)
            => serviceModels.Select(serviceModel => serviceModel.ToDataObject());
    }
}
