using System.Collections.Generic;
using System.Linq;

using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service.Mapping
{
    static class ChannelDefinitionMapping
    {
        internal static ChannelDefinition ToServiceModel(this ChannelDefinitionEntity dataObject)
        {
            ChannelDefinition serviceModel = new ChannelDefinition();
            serviceModel.Id = dataObject.Id;
            serviceModel.IsEnabled = dataObject.IsEnabled;
            serviceModel.Name = new ChannelName(dataObject.Name, dataObject.Country, dataObject.Aliases);
            serviceModel.Country = dataObject.Country;
            serviceModel.GroupId = dataObject.GroupId;
            serviceModel.LogoUrl = dataObject.LogoUrl;

            return serviceModel;
        }

        internal static ChannelDefinitionEntity ToDataObject(this ChannelDefinition serviceModel)
        {
            ChannelDefinitionEntity dataObject = new ChannelDefinitionEntity();
            dataObject.Id = serviceModel.Id;
            dataObject.IsEnabled = serviceModel.IsEnabled;
            dataObject.Name = serviceModel.Name.Value;
            dataObject.Country = serviceModel.Country;
            dataObject.GroupId = serviceModel.GroupId;
            dataObject.LogoUrl = serviceModel.LogoUrl;
            dataObject.Aliases = serviceModel.Name.Aliases.ToList();

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
