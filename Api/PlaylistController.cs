using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using IptvPlaylistFetcher.Service;

namespace IptvPlaylistFetcher.Controllers
{
    [ApiController]
    public class PlaylistController : ControllerBase
    {
        readonly IPlaylistFetcher service;

        public PlaylistController(IPlaylistFetcher service)
        {
            this.service = service;
        }

        [HttpGet("playlist.m3u")]
        public ActionResult<string> GetAccount()
        {
            return service.GetPlaylistFile();
        }
    }
}
