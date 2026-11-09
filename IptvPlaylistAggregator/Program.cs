using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciLog;
using NuciLog.Core;

using NuciWeb.HTTP;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.Logging;
using IptvPlaylistAggregator.Service;

namespace IptvPlaylistAggregator
{
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            IConfiguration configuration = LoadConfiguration();

            ApplicationSettings applicationSettings = new();
            CacheSettings cacheSettings = new();
            DataStoreSettings dataStoreSettings = new();

            configuration.Bind(nameof(ApplicationSettings), applicationSettings);
            configuration.Bind(nameof(CacheSettings), cacheSettings);
            configuration.Bind(nameof(DataStoreSettings), dataStoreSettings);

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton(applicationSettings)
                .AddSingleton(cacheSettings)
                .AddSingleton(dataStoreSettings)
                .AddNuciLoggerSettings(configuration)
                .AddIptvServices(dataStoreSettings)
                .AddSingleton<ILogger, NuciLogger>()
                .BuildServiceProvider();

            ILogger logger = serviceProvider.GetRequiredService<ILogger>();
            ICacheManager cacheManager = serviceProvider.GetRequiredService<ICacheManager>();

            logger.Info(Operation.StartUp, OperationStatus.Success);

            if (!NetworkUtils.HasInternetAccess())
            {
                logger.Fatal(Operation.StartUp, OperationStatus.Failure, "No internet access detected. Please check the network connection.");
                return;
            }

            try
            {
                IPlaylistAggregator aggregator = serviceProvider.GetRequiredService<IPlaylistAggregator>();

                string playlistFile = aggregator.GatherPlaylist();
                File.WriteAllText(applicationSettings.OutputPlaylistPath, playlistFile);
            }
            catch (AggregateException aggregateException)
            {
                LogInnerExceptions(logger, aggregateException);
            }
            catch (Exception exception)
            {
                logger.Fatal(Operation.Unknown, OperationStatus.Failure, exception);
            }

            SaveCacheToDisk(logger, cacheManager);

            logger.Info(Operation.ShutDown, OperationStatus.Success);
        }

        private static IConfiguration LoadConfiguration()
            => new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

        private static void LogInnerExceptions(ILogger logger, AggregateException exception)
        {
            foreach (Exception innerException in exception.InnerExceptions)
            {
                if (innerException is AggregateException aggregateInnerException)
                {
                    LogInnerExceptions(logger, aggregateInnerException);
                }
                else
                {
                    logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
                }
            }
        }

        private static void SaveCacheToDisk(ILogger logger, ICacheManager cacheManager)
        {
            logger.Info(MyOperation.CacheSaving, OperationStatus.Started);
            cacheManager.SaveCacheToDisk();
            logger.Debug(MyOperation.CacheSaving, OperationStatus.Success);
        }
    }
}
