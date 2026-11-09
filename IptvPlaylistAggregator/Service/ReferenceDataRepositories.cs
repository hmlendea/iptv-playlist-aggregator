using NuciDAL.Repositories;

using IptvPlaylistAggregator.DataAccess.DataObjects;

namespace IptvPlaylistAggregator.Service
{
    /// <summary>
    /// Groups the XML reference-data repositories that the aggregation pipeline reads.
    /// </summary>
    /// <param name="Channels">The repository of curated channel definitions.</param>
    /// <param name="Groups">The repository of output groups.</param>
    /// <param name="PlaylistProviders">The repository of playlist providers.</param>
    public sealed class ReferenceDataRepositories(
        IFileRepository<ChannelDefinitionDataObject> channels,
        IFileRepository<GroupDataObject> groups,
        IFileRepository<PlaylistProviderDataObject> playlistProviders)
    {
        /// <summary>
        /// Gets the repository of curated channel definitions.
        /// </summary>
        public IFileRepository<ChannelDefinitionDataObject> Channels { get; } = channels;

        /// <summary>
        /// Gets the repository of output groups.
        /// </summary>
        public IFileRepository<GroupDataObject> Groups { get; } = groups;

        /// <summary>
        /// Gets the repository of playlist providers.
        /// </summary>
        public IFileRepository<PlaylistProviderDataObject> PlaylistProviders { get; } = playlistProviders;
    }
}
