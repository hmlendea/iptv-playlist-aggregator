using System.Collections.Generic;
using System.Linq;

using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service.Mapping
{
    static class GroupMapping
    {
        internal static Group ToServiceModel(this GroupEntity dataObject)
        {
            Group serviceModel = new Group();
            serviceModel.Id = dataObject.Id;
            serviceModel.IsEnabled = dataObject.IsEnabled;
            serviceModel.Name = dataObject.Name;
            serviceModel.Priority = dataObject.Priority;

            return serviceModel;
        }

        internal static GroupEntity ToDataObject(this Group serviceModel)
        {
            GroupEntity dataObject = new GroupEntity();
            dataObject.Id = serviceModel.Id;
            dataObject.IsEnabled = serviceModel.IsEnabled;
            dataObject.Name = serviceModel.Name;
            dataObject.Priority = serviceModel.Priority;

            return dataObject;
        }

        internal static IEnumerable<Group> ToServiceModels(this IEnumerable<GroupEntity> dataObjects)
        {
            IEnumerable<Group> serviceModels = dataObjects.Select(dataObject => dataObject.ToServiceModel());

            return serviceModels;
        }

        internal static IEnumerable<GroupEntity> ToEntities(this IEnumerable<Group> serviceModels)
        {
            IEnumerable<GroupEntity> dataObjects = serviceModels.Select(serviceModel => serviceModel.ToDataObject());

            return dataObjects;
        }
    }
}
