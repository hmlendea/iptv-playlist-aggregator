using System.Collections.Generic;
using System.Linq;

using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service.Mapping
{
    internal static class GroupMapping
    {
        internal static Group ToServiceModel(this GroupEntity dataObject) => new()
        {
            Id = dataObject.Id,
            IsEnabled = dataObject.IsEnabled,
            Name = dataObject.Name,
            Priority = dataObject.Priority
        };

        internal static GroupEntity ToDataObject(this Group serviceModel) => new()
        {
            Id = serviceModel.Id,
            IsEnabled = serviceModel.IsEnabled,
            Name = serviceModel.Name,
            Priority = serviceModel.Priority
        };

        internal static IEnumerable<Group> ToServiceModels(this IEnumerable<GroupEntity> dataObjects)
            => dataObjects.Select(dataObject => dataObject.ToServiceModel());

        internal static IEnumerable<GroupEntity> ToEntities(this IEnumerable<Group> serviceModels)
            => serviceModels.Select(serviceModel => serviceModel.ToDataObject());
    }
}
