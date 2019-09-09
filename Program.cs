using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciDAL.Repositories;
using NuciLog;
using NuciLog.Core;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service;

namespace IptvPlaylistAggregator
{
    public class Program
    {
        static ApplicationSettings applicationSettings;
        static CacheSettings cacheSettings;
        static DataStoreSettings dataStoreSettings;

        public static void Main(string[] args)
        {
            IConfiguration config = LoadConfiguration();

            applicationSettings = new ApplicationSettings();
            cacheSettings = new CacheSettings();
            dataStoreSettings = new DataStoreSettings();

            config.Bind(nameof(ApplicationSettings), applicationSettings);
            config.Bind(nameof(CacheSettings), cacheSettings);
            config.Bind(nameof(DataStoreSettings), dataStoreSettings);

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton(applicationSettings)
                .AddSingleton(cacheSettings)
                .AddSingleton(dataStoreSettings)
                .AddSingleton<IFileDownloader, FileDownloader>()
                .AddSingleton<IPlaylistAggregator, PlaylistAggregator>()
                .AddSingleton<IPlaylistFetcher, PlaylistFetcher>()
                .AddSingleton<IPlaylistFileBuilder, PlaylistFileBuilder>()
                .AddSingleton<IChannelMatcher, ChannelMatcher>()
                .AddSingleton<IMediaSourceChecker, MediaSourceChecker>()
                .AddSingleton<IRepository<ChannelDefinitionEntity>>(s => new XmlRepository<ChannelDefinitionEntity>(dataStoreSettings.ChannelStorePath))
                .AddSingleton<IRepository<GroupEntity>>(s => new XmlRepository<GroupEntity>(dataStoreSettings.GroupStorePath))
                .AddSingleton<IRepository<PlaylistProviderEntity>>(s => new XmlRepository<PlaylistProviderEntity>(dataStoreSettings.PlaylistProviderStorePath))
                .AddSingleton<ILogger, NuciLogger>()
                .BuildServiceProvider();
            
            ILogger logger = serviceProvider.GetService<ILogger>();
            logger.Info(Operation.StartUp, OperationStatus.Success);

            try
            {
                IPlaylistAggregator aggregator = serviceProvider.GetService<IPlaylistAggregator>();

                string playlistFile = aggregator.GatherPlaylist();
                File.WriteAllText(applicationSettings.OutputPlaylistPath, playlistFile);
            }
            catch (Exception ex)
            {
                logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
            }

            logger.Info(Operation.ShutDown, OperationStatus.Success);
        }
        
        static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }
    }
}
