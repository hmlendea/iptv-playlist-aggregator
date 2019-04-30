using System.Collections.Generic;
using System.Linq;

using IptvPlaylistFetcher.DataAccess.DataObjects;
using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service.Mapping
{
    static class ChannelDefinitionMapping
    {
        internal static ChannelDefinition ToServiceModel(this ChannelDefinitionEntity dataObject)
        {
            ChannelDefinition serviceModel = new ChannelDefinition();
            serviceModel.Id = dataObject.Id;
            serviceModel.IsEnabled = dataObject.IsEnabled;
            serviceModel.Name = dataObject.Name;
            serviceModel.GroupId = dataObject.GroupId;
            serviceModel.LogoUrl = dataObject.LogoUrl;
            serviceModel.Aliases = dataObject.Aliases;

            return serviceModel;
        }

        internal static ChannelDefinitionEntity ToDataObject(this ChannelDefinition serviceModel)
        {
            ChannelDefinitionEntity dataObject = new ChannelDefinitionEntity();
            dataObject.Id = serviceModel.Id;
            dataObject.IsEnabled = serviceModel.IsEnabled;
            dataObject.Name = serviceModel.Name;
            dataObject.GroupId = serviceModel.GroupId;
            dataObject.LogoUrl = serviceModel.LogoUrl;
            dataObject.Aliases = serviceModel.Aliases;

            return dataObject;
        }

        internal static IEnumerable<ChannelDefinition> ToServiceModels(this IEnumerable<ChannelDefinitionEntity> dataObjects)
        {
            IEnumerable<ChannelDefinition> serviceModels = dataObjects.Select(dataObject => dataObject.ToServiceModel());

            return serviceModels;
        }

        internal static IEnumerable<ChannelDefinitionEntity> ToEntities(this IEnumerable<ChannelDefinition> serviceModels)
        {
            IEnumerable<ChannelDefinitionEntity> dataObjects = serviceModels.Select(serviceModel => serviceModel.ToDataObject());

            return dataObjects;
        }
    }
}
