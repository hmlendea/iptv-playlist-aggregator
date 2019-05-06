using System.Collections.Generic;
using System.Linq;

using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service.Mapping
{
    static class PlaylistProviderMapping
    {
        internal static PlaylistProvider ToServiceModel(this PlaylistProviderEntity dataObject)
        {
            PlaylistProvider serviceModel = new PlaylistProvider();
            serviceModel.Id = dataObject.Id;
            serviceModel.IsEnabled = dataObject.IsEnabled;
            serviceModel.Priority = dataObject.Priority;
            serviceModel.Name = dataObject.Name;
            serviceModel.UrlFormat = dataObject.UrlFormat;
            serviceModel.ChannelNameOverride = dataObject.ChannelNameOverride;

            return serviceModel;
        }

        internal static PlaylistProviderEntity ToDataObject(this PlaylistProvider serviceModel)
        {
            PlaylistProviderEntity dataObject = new PlaylistProviderEntity();
            dataObject.Id = serviceModel.Id;
            dataObject.IsEnabled = serviceModel.IsEnabled;
            dataObject.Priority = serviceModel.Priority;
            dataObject.Name = serviceModel.Name;
            dataObject.UrlFormat = serviceModel.UrlFormat;
            dataObject.ChannelNameOverride = serviceModel.ChannelNameOverride;

            return dataObject;
        }

        internal static IEnumerable<PlaylistProvider> ToServiceModels(this IEnumerable<PlaylistProviderEntity> dataObjects)
        {
            IEnumerable<PlaylistProvider> serviceModels = dataObjects.Select(dataObject => dataObject.ToServiceModel());

            return serviceModels;
        }

        internal static IEnumerable<PlaylistProviderEntity> ToEntities(this IEnumerable<PlaylistProvider> serviceModels)
        {
            IEnumerable<PlaylistProviderEntity> dataObjects = serviceModels.Select(serviceModel => serviceModel.ToDataObject());

            return dataObjects;
        }
    }
}
