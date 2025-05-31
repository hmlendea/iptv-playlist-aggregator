using System.Collections.Generic;
using System.Linq;

using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service.Mapping
{
    internal static class ChannelDefinitionMapping
    {
        internal static ChannelDefinition ToServiceModel(this ChannelDefinitionEntity dataObject) => new()
        {
            Id = dataObject.Id,
            IsEnabled = dataObject.IsEnabled,
            Name = new(dataObject.Name, dataObject.Country, dataObject.Aliases),
            Country = dataObject.Country,
            GroupId = dataObject.GroupId,
            LogoUrl = dataObject.LogoUrl
        };

        internal static ChannelDefinitionEntity ToDataObject(this ChannelDefinition serviceModel) => new()
        {
            Id = serviceModel.Id,
            IsEnabled = serviceModel.IsEnabled,
            Name = serviceModel.Name.Value,
            Country = serviceModel.Country,
            GroupId = serviceModel.GroupId,
            LogoUrl = serviceModel.LogoUrl,
            Aliases = serviceModel.Name.Aliases.ToList()
        };

        internal static IEnumerable<ChannelDefinition> ToServiceModels(this IEnumerable<ChannelDefinitionEntity> dataObjects)
            => dataObjects.Select(dataObject => dataObject.ToServiceModel());

        internal static IEnumerable<ChannelDefinitionEntity> ToEntities(this IEnumerable<ChannelDefinition> serviceModels)
            => serviceModels.Select(serviceModel => serviceModel.ToDataObject());
    }
}
