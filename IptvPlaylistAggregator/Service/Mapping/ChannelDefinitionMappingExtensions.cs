namespace IptvPlaylistAggregator.Service.Mapping
{
    internal static class ChannelDefinitionMappingExtensions
    {
        internal static ChannelDefinition ToDomainModel(this ChannelDefinitionDataObject dataObject) => new()
        {
            Id = dataObject.Id,
            IsEnabled = dataObject.IsEnabled,
            Name = new(dataObject.Name, dataObject.Country, dataObject.Aliases),
            Country = dataObject.Country,
            GroupId = dataObject.GroupId,
            LogoUrl = dataObject.LogoUrl
        };

        internal static ChannelDefinitionDataObject ToDataObject(this ChannelDefinition channelDefinition) => new()
        {
            Id = channelDefinition.Id,
            IsEnabled = channelDefinition.IsEnabled,
            Name = channelDefinition.Name.Value,
            Country = channelDefinition.Country,
            GroupId = channelDefinition.GroupId,
            LogoUrl = channelDefinition.LogoUrl,
            Aliases = [.. channelDefinition.Name.Aliases]
        };

        internal static IEnumerable<ChannelDefinition> ToDomainModels(
            this IEnumerable<ChannelDefinitionDataObject> dataObjects)
            => dataObjects.Select(dataObject => dataObject.ToDomainModel());

        internal static IEnumerable<ChannelDefinitionDataObject> ToDataObjects(
            this IEnumerable<ChannelDefinition> channelDefinitions)
            => channelDefinitions.Select(channelDefinition => channelDefinition.ToDataObject());
    }
}
