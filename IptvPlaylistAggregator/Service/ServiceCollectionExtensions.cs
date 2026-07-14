using Microsoft.Extensions.DependencyInjection;

using NuciDAL.Repositories;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;

namespace IptvPlaylistAggregator.Service
{
    internal static class ServiceCollectionExtensions
    {
        internal static IServiceCollection AddIptvServices(
            this IServiceCollection services,
            DataStoreSettings dataStoreSettings)
        {
            services
                .AddSingleton<ICacheManager, CacheManager>()
                .AddSingleton<IFileDownloader, FileDownloader>()
                .AddSingleton<IPlaylistAggregator, PlaylistAggregator>()
                .AddSingleton<IPlaylistFetcher, PlaylistFetcher>()
                .AddSingleton<IPlaylistFileBuilder, PlaylistFileBuilder>()
                .AddSingleton<IChannelMatcher, ChannelMatcher>()
                .AddSingleton<IMediaSourceChecker, MediaSourceChecker>()
                .AddSingleton<IFileRepository<ChannelDefinitionEntity>>(
                    serviceProvider => new XmlRepository<ChannelDefinitionEntity>(
                        dataStoreSettings.ChannelStorePath))
                .AddSingleton<IFileRepository<GroupEntity>>(
                    serviceProvider => new XmlRepository<GroupEntity>(
                        dataStoreSettings.GroupStorePath))
                .AddSingleton<IFileRepository<PlaylistProviderEntity>>(
                    serviceProvider => new XmlRepository<PlaylistProviderEntity>(
                        dataStoreSettings.PlaylistProviderStorePath));

            return services;
        }
    }
}
