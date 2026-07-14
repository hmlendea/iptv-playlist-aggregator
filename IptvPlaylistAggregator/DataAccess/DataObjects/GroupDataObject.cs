namespace IptvPlaylistAggregator.DataAccess.DataObjects
{
    public sealed class GroupDataObject : EntityBase
    {
        public bool IsEnabled { get; set; }

        public string Name { get; set; }

        public int Priority { get; set; }

        public GroupDataObject() => Priority = int.MaxValue;
    }
}
