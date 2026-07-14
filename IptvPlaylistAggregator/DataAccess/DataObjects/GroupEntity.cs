using NuciDAL.DataObjects;

namespace IptvPlaylistAggregator.DataAccess.DataObjects
{
    public sealed class GroupEntity : EntityBase
    {
        public bool IsEnabled { get; set; }

        public string Name { get; set; }

        public int Priority { get; set; }

        public GroupEntity() => Priority = int.MaxValue;
    }
}
