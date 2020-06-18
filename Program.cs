using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciDAL.Repositories;
using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Logging;
using IptvPlaylistAggregator.Service;

namespace IptvPlaylistAggregator
{
    public class Program
    {
        static ApplicationSettings applicationSettings;
        static CacheSettings cacheSettings;
        static DataStoreSettings dataStoreSettings;
        static NuciLoggerSettings nuciLoggerSettings;

        static ILogger logger;
        static ICacheManager cacheManager;

        public static void Main(string[] args)
        {
            IConfiguration config = LoadConfiguration();

            applicationSettings = new ApplicationSettings();
            cacheSettings = new CacheSettings();
            dataStoreSettings = new DataStoreSettings();
            nuciLoggerSettings = new NuciLoggerSettings();

            config.Bind(nameof(ApplicationSettings), applicationSettings);
            config.Bind(nameof(CacheSettings), cacheSettings);
            config.Bind(nameof(DataStoreSettings), dataStoreSettings);
            config.Bind(nameof(NuciLoggerSettings), nuciLoggerSettings);

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton(applicationSettings)
                .AddSingleton(cacheSettings)
                .AddSingleton(dataStoreSettings)
                .AddSingleton(nuciLoggerSettings)
                .AddSingleton<ICacheManager, CacheManager>()
                .AddSingleton<IDnsResolver, DnsResolver>()
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
            
            logger = serviceProvider.GetService<ILogger>();
            cacheManager = serviceProvider.GetService<ICacheManager>();

            logger.Info(Operation.StartUp, OperationStatus.Success);

            try
            {
                IPlaylistAggregator aggregator = serviceProvider.GetService<IPlaylistAggregator>();

                string playlistFile = aggregator.GatherPlaylist();
                File.WriteAllText(applicationSettings.OutputPlaylistPath, playlistFile);
            }
            catch (AggregateException ex)
            {
                foreach (Exception innerException in ex.InnerExceptions)
                {
                    logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
            }

            SaveCacheToDisk();

            logger.Info(Operation.ShutDown, OperationStatus.Success);
        }
        
        static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }

        static void SaveCacheToDisk()
        {
            logger.Info(MyOperation.CacheSaving, OperationStatus.Started);
            cacheManager.SaveCacheToDisk();
            logger.Debug(MyOperation.CacheSaving, OperationStatus.Success);
        }
    }
}
