[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html) [![Build Status](https://github.com/hmlendea/iptv-playlist-aggregator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/iptv-playlist-aggregator/actions/workflows/dotnet.yml) [![Latest GitHub release](https://img.shields.io/github/v/release/hmlendea/iptv-playlist-aggregator)](https://github.com/hmlendea/iptv-playlist-aggregator/releases/latest)

# IPTV Playlist Aggregator

IPTV Playlist Aggregator is a .NET console application that downloads playlists from multiple providers and merges them into a single curated M3U file.

It is designed for users who want one stable playlist, with their own channel metadata and grouping rules, even when source playlists are noisy or inconsistent.

## What It Does

- Downloads M3U playlists from multiple providers.
- Matches provider channels against your channel definitions (including aliases).
- Keeps one playable stream per configured channel.
- Writes a final unified M3U playlist to a configurable path.
- Optionally includes unmatched channels under an unknown grouping.
- Supports provider/date-based playlist URLs.
- Caches downloaded and parsed data to reduce repeated work.

## How Aggregation Works

1. Load groups, channel definitions, and providers from XML files.
2. Fetch enabled providers and parse their playlists.
3. Remove duplicate stream URLs.
4. Match each configured channel against provider channel names (and optional country context).
5. Keep the first playable media source for each matched channel.
6. Optionally append unmatched playable channels.
7. Generate a single output M3U file.

## Project Structure

- `IptvPlaylistAggregator/`: main app
- `IptvPlaylistAggregator/Data/`: sample data files (`channels.xml`, `groups.xml`, `providers.xml`)
- `IptvPlaylistAggregator/Service/`: aggregation, matching, fetch, and M3U build logic
- `IptvPlaylistAggregator.UnitTests/`: unit tests

## Requirements

- .NET SDK 10.0+
- Internet access (required at runtime to fetch source playlists)

## Quick Start

From the repository root:

```bash
dotnet restore
dotnet build IptvPlaylistAggregator.sln
dotnet run --project IptvPlaylistAggregator/IptvPlaylistAggregator.csproj
```

By default, the output playlist is written to `result.m3u` (configured in `appsettings.json`).

## Build and Publish

### Publish for current OS/architecture

```bash
dotnet publish IptvPlaylistAggregator/IptvPlaylistAggregator.csproj -c Release
```

### Publish for a specific runtime

```bash
dotnet publish IptvPlaylistAggregator/IptvPlaylistAggregator.csproj -c Release -r <RID>
```

Example RIDs: `linux-x64`, `linux-arm64`, `win-x64`.

## Configuration

Runtime settings are loaded from `IptvPlaylistAggregator/appsettings.json`.

### `applicationSettings`

- `outputPlaylistPath`: where the merged M3U file is written
- `daysToCheck`: number of days to look back for dated provider URLs
- `canIncludeUnmatchedChannels`: include channels not matched to your definitions
- `areTvGuideTagsEnabled`: include TV guide tags in `#EXTINF`
- `arePlaylistDetailsTagsEnabled`: include source playlist metadata tags

### `cacheSettings`

- `cacheDirectoryPath`: cache folder path
- `hostCacheTimeout`: cache timeout for host checks
- `streamAliveStatusCacheTimeout`: timeout for alive stream status cache
- `streamDeadStatusCacheTimeout`: timeout for dead stream status cache
- `streamUnauthorisedStatusCacheTimeout`: timeout for unauthorized stream status cache
- `streamNotFoundStatusCacheTimeout`: timeout for not-found stream status cache

### `dataStoreSettings`

- `channelStorePath`: XML path for channel definitions
- `groupStorePath`: XML path for groups
- `playlistProviderStorePath`: XML path for providers

## Data Files

All data stores are XML arrays of entities.

### Channels (`channels.xml`)

Entity: `ChannelDefinitionEntity`

- `Id` (string): channel identifier, also used as TVG ID in output
- `IsEnabled` (bool): include/exclude channel
- `Name` (string): final display name
- `Country` (string, optional): country metadata and matching hint
- `GroupId` (string): group reference
- `LogoUrl` (string, optional): logo URL
- `Aliases` (string list): accepted source name variants for matching

### Groups (`groups.xml`)

Entity: `GroupEntity`

- `Id` (string): group identifier
- `IsEnabled` (bool): include/exclude group
- `Name` (string): display name
- `Priority` (int): sort order (lower appears first)

### Providers (`providers.xml`)

Entity: `PlaylistProviderEntity`

- `Id` (string): provider identifier
- `IsEnabled` (bool): enable/disable provider
- `Priority` (int): provider processing order (lower is earlier)
- `AllowCaching` (bool): enable playlist caching for this provider
- `Name` (string): provider display name
- `UrlFormat` (string): provider URL, optionally with date placeholder
- `Country` (string, optional): provider country hint
- `ChannelNameOverride` (string, optional): force all channels from provider to this name

Date placeholder example in `UrlFormat`:

```text
https://example.com/playlists/{0:yyyy-MM-dd}.m3u
```

## Run as a Linux systemd Service

The app is a console executable, so it can be scheduled with a systemd timer.

Create `/etc/systemd/system/iptv-playlist-aggregator.service`:

```ini
[Unit]
Description=IPTV Playlist Aggregator

[Service]
WorkingDirectory=/absolute/path/to/IptvPlaylistAggregator
ExecStart=/absolute/path/to/IptvPlaylistAggregator/IptvPlaylistAggregator
User=your-user
```

Create `/etc/systemd/system/iptv-playlist-aggregator.timer`:

```ini
[Unit]
Description=Periodically aggregate IPTV playlists

[Timer]
OnBootSec=5min
OnUnitActiveSec=50min

[Install]
WantedBy=timers.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now iptv-playlist-aggregator.timer
```

## Development

Run tests:

```bash
dotnet test IptvPlaylistAggregator.UnitTests/IptvPlaylistAggregator.UnitTests.csproj
```

## Target Framework

The project currently targets `.NET 10.0`.

## Contributing

Contributions are welcome. Please keep changes cross-platform and consistent with existing C# coding style.

## Legal Notice

This software aggregates playlist sources. You are responsible for ensuring your usage complies with local laws and content licensing requirements.

## License

This project is licensed under the `GNU General Public License v3.0` or later. See [LICENSE](./LICENSE) for details.