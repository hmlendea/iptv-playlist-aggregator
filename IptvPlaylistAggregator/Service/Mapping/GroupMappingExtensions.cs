using System.Collections.Generic;
using System.Linq;

using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.Service.Mapping
{
    internal static class GroupMappingExtensions
    {
        internal static Group ToDomainModel(this GroupDataObject dataObject) => new()
        {
            Id = dataObject.Id,
            IsEnabled = dataObject.IsEnabled,
            Name = dataObject.Name,
            Priority = dataObject.Priority
        };

        internal static GroupDataObject ToDataObject(this Group group) => new()
        {
            Id = group.Id,
            IsEnabled = group.IsEnabled,
            Name = group.Name,
            Priority = group.Priority
        };

        internal static IEnumerable<Group> ToDomainModels(this IEnumerable<GroupDataObject> dataObjects)
            => dataObjects.Select(dataObject => dataObject.ToDomainModel());

        internal static IEnumerable<GroupDataObject> ToDataObjects(this IEnumerable<Group> groups)
            => groups.Select(group => group.ToDataObject());
    }
}
