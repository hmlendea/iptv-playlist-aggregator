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
                .AddSingleton<IFileRepository<ChannelDefinitionDataObject>>(
                    serviceProvider => new XmlRepository<ChannelDefinitionDataObject>(
                        dataStoreSettings.ChannelStorePath))
                .AddSingleton<IFileRepository<GroupDataObject>>(
                    serviceProvider => new XmlRepository<GroupDataObject>(
                        dataStoreSettings.GroupStorePath))
                .AddSingleton<IFileRepository<PlaylistProviderDataObject>>(
                    serviceProvider => new XmlRepository<PlaylistProviderDataObject>(
                        dataStoreSettings.PlaylistProviderStorePath));

            return services;
        }
    }
}
