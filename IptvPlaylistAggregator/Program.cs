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
        private static ApplicationSettings applicationSettings;
        private static CacheSettings cacheSettings;
        private static DataStoreSettings dataStoreSettings;
        private static NuciLoggerSettings nuciLoggerSettings;

        private static ILogger logger;
        private static ICacheManager cacheManager;

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
                .AddSingleton<IFileDownloader, FileDownloader>()
                .AddSingleton<IPlaylistAggregator, PlaylistAggregator>()
                .AddSingleton<IPlaylistFetcher, PlaylistFetcher>()
                .AddSingleton<IPlaylistFileBuilder, PlaylistFileBuilder>()
                .AddSingleton<IChannelMatcher, ChannelMatcher>()
                .AddSingleton<IMediaSourceChecker, MediaSourceChecker>()
                .AddSingleton<IFileRepository<ChannelDefinitionEntity>>(s => new XmlRepository<ChannelDefinitionEntity>(dataStoreSettings.ChannelStorePath))
                .AddSingleton<IFileRepository<GroupEntity>>(s => new XmlRepository<GroupEntity>(dataStoreSettings.GroupStorePath))
                .AddSingleton<IFileRepository<PlaylistProviderEntity>>(s => new XmlRepository<PlaylistProviderEntity>(dataStoreSettings.PlaylistProviderStorePath))
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
                LogInnerExceptions(ex);
            }
            catch (Exception ex)
            {
                logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
            }

            SaveCacheToDisk();

            logger.Info(Operation.ShutDown, OperationStatus.Success);
        }

        private static IConfiguration LoadConfiguration()
            => new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

        private static void LogInnerExceptions(AggregateException exception)
        {
            foreach (Exception innerException in exception.InnerExceptions)
            {
                if (innerException is not AggregateException)
                {
                    logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
                }
                else
                {
                    LogInnerExceptions(innerException as AggregateException);
                }
            }
        }

        private static void SaveCacheToDisk()
        {
            logger.Info(MyOperation.CacheSaving, OperationStatus.Started);
            cacheManager.SaveCacheToDisk();
            logger.Debug(MyOperation.CacheSaving, OperationStatus.Success);
        }
    }
}
