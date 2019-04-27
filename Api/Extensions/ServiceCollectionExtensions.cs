using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using IptvPlaylistFetcher.Core.Configuration;
using IptvPlaylistFetcher.DataAccess.DataObjects;
using IptvPlaylistFetcher.DataAccess.Repositories;
using IptvPlaylistFetcher.Service;

namespace IptvPlaylistFetcher.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfigurations(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ApplicationSettings settings = new ApplicationSettings();
            configuration.Bind(nameof(ApplicationSettings), settings);
            services.AddSingleton(settings);

            return services;
        }

        public static IServiceCollection AddCustomServices(this IServiceCollection services)
        {
            return services
                .AddScoped<IPlaylistFetcher, PlaylistFetcher>()
                .AddScoped<IPlaylistFileBuilder, PlaylistFileBuilder>()
                .AddScoped<IMediaStreamStatusChecker, MediaStreamStatusChecker>()
                .AddScoped<IChannelDefinitionRepository, ChannelDefinitionRepository>()
                .AddScoped<IPlaylistProviderRepository, PlaylistProviderRepository>();
        }
    }
}
