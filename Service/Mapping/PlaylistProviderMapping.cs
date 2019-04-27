using System.Collections.Generic;
using System.Linq;

using IptvPlaylistFetcher.DataAccess.DataObjects;
using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service.Mapping
{
    static class PlaylistProviderMapping
    {
        internal static PlaylistProvider ToServiceModel(this PlaylistProviderEntity dataObject)
        {
            PlaylistProvider serviceModel = new PlaylistProvider();
            serviceModel.Id = dataObject.Id;
            serviceModel.Name = dataObject.Name;
            serviceModel.UrlFormat = dataObject.UrlFormat;

            return serviceModel;
        }

        internal static PlaylistProviderEntity ToDataObject(this PlaylistProvider serviceModel)
        {
            PlaylistProviderEntity dataObject = new PlaylistProviderEntity();
            dataObject.Id = serviceModel.Id;
            dataObject.Name = serviceModel.Name;
            dataObject.UrlFormat = serviceModel.UrlFormat;

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
