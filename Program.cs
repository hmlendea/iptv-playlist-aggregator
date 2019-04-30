using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using IptvPlaylistFetcher.Configuration;
using IptvPlaylistFetcher.DataAccess.Repositories;
using IptvPlaylistFetcher.Service;

namespace IptvPlaylistFetcher
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
                .AddSingleton<IPlaylistAggregator, PlaylistAggregator>()
                .AddSingleton<IPlaylistFetcher, PlaylistFetcher>()
                .AddSingleton<IPlaylistFileBuilder, PlaylistFileBuilder>()
                .AddSingleton<IMediaStreamStatusChecker, MediaStreamStatusChecker>()
                .AddSingleton<IChannelDefinitionRepository, ChannelDefinitionRepository>()
                .AddSingleton<IGroupRepository, GroupRepository>()
                .AddSingleton<IPlaylistProviderRepository, PlaylistProviderRepository>()
                .BuildServiceProvider();

            IPlaylistAggregator aggregator = serviceProvider.GetService<IPlaylistAggregator>();

            string playlistFile = aggregator.GatherPlaylist();
            File.WriteAllText(applicationSettings.OutputPlaylistPath, playlistFile);
        }
        
        static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }
    }
}
