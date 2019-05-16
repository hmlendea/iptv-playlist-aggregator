using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciLog;
using NuciLog.Core;

using IptvPlaylistAggregator.Communication;
using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.Repositories;
using IptvPlaylistAggregator.Service;

namespace IptvPlaylistAggregator
{
    public class Program
    {
        static ApplicationSettings applicationSettings;

        public static void Main(string[] args)
        {
            IConfiguration config = LoadConfiguration();

            applicationSettings = new ApplicationSettings();
            config.Bind(nameof(ApplicationSettings), applicationSettings);

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton(applicationSettings)
                .AddSingleton<IFileDownloader, FileDownloader>()
                .AddSingleton<IPlaylistAggregator, PlaylistAggregator>()
                .AddSingleton<IPlaylistFetcher, PlaylistFetcher>()
                .AddSingleton<IPlaylistFileBuilder, PlaylistFileBuilder>()
                .AddSingleton<IMediaSourceChecker, MediaSourceChecker>()
                .AddSingleton<IChannelDefinitionRepository, ChannelDefinitionRepository>()
                .AddSingleton<IGroupRepository, GroupRepository>()
                .AddSingleton<IPlaylistProviderRepository, PlaylistProviderRepository>()
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
