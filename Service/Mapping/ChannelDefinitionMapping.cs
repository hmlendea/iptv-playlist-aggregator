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
            serviceModel.Name = dataObject.Name;
            serviceModel.Category = dataObject.Category;
            serviceModel.Aliases = dataObject.Aliases;

            return serviceModel;
        }

        internal static ChannelDefinitionEntity ToDataObject(this ChannelDefinition serviceModel)
        {
            ChannelDefinitionEntity dataObject = new ChannelDefinitionEntity();
            dataObject.Id = serviceModel.Id;
            dataObject.Name = serviceModel.Name;
            dataObject.Category = serviceModel.Category;
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
