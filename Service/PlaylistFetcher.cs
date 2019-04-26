using System.Linq;

using IptvPlaylistFetcher.DataAccess.Repositories;
using IptvPlaylistFetcher.Service.Models;

namespace IptvPlaylistFetcher.Service
{
    public sealed class PlaylistFetcher : IPlaylistFetcher
    {
        readonly IPlaylistFileBuilder playlistFileBuilder;
        readonly IChannelDefinitionRepository channelRepository;
        readonly IPlaylistProviderRepository playlistProviderRepository;

        public PlaylistFetcher(
            IPlaylistFileBuilder playlistFileBuilder,
            IChannelDefinitionRepository channelRepository,
            IPlaylistProviderRepository playlistProviderRepository)
        {
            this.playlistFileBuilder = playlistFileBuilder;
            this.channelRepository = channelRepository;
            this.playlistProviderRepository = playlistProviderRepository;
        }

        public string GetPlaylistFile()
        {
            Playlist playlist = new Playlist();

            playlist.Channels = channelRepository
                .GetAll()
                .Select(x => new Channel
                {
                    Name = x.Name,
                    Url = "dummy"
                });

            return playlistFileBuilder.BuildFile(playlist);
        }
    }
}
