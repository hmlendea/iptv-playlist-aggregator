[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/iptv-playlist-aggregator)](https://github.com/hmlendea/iptv-playlist-aggregator/releases/latest)
[![Build Status](https://github.com/hmlendea/iptv-playlist-aggregator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/iptv-playlist-aggregator/actions/workflows/dotnet.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://gnu.org/licenses/gpl-3.0)

# IPTV Playlist Aggregator

IPTV Playlist Aggregator is a .NET console application that downloads playlists from multiple providers and merges them into a single curated M3U file.

It is designed for users who want one stable playlist, with their own channel metadata and grouping rules, even when source playlists are noisy or inconsistent.

## Features

- Downloads M3U playlists from multiple configurable providers.
- Matches provider channels against your channel definitions (including aliases).
- Keeps one playable stream per configured channel.
- Writes a final unified M3U playlist to a configurable path.
- Optionally includes unmatched channels.
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

## Configuration

All settings are loaded from `appsettings.json`. The following keys are recognised:

| Section | Key | Description |
|---------|-----|-------------|
| `nuciLoggerSettings` | `logFilePath` | Path of the log file |
| `nuciLoggerSettings` | `isFileOutputEnabled` | Whether to write logs to file |
| `nuciLoggerSettings` | `minimumLevel` | Minimum log level to record |
| `applicationSettings` | `outputPlaylistPath` | Path where the merged M3U file is written |
| `applicationSettings` | `daysToCheck` | Number of days to look back for dated provider URLs |
| `applicationSettings` | `areUnmatchedChannelsIncluded` | Whether to include channels not matched to your definitions |
| `applicationSettings` | `areTvGuideTagsEnabled` | Whether to include TV guide tags in `#EXTINF` |
| `applicationSettings` | `arePlaylistDetailsTagsEnabled` | Whether to include source playlist metadata tags |
| `cacheSettings` | `cacheDirectoryPath` | Cache folder path |
| `cacheSettings` | `streamAliveStatusCacheTimeout` | Cache timeout in seconds for alive stream status |
| `cacheSettings` | `streamDeadStatusCacheTimeout` | Cache timeout in seconds for dead stream status |
| `cacheSettings` | `streamUnauthorisedStatusCacheTimeout` | Cache timeout in seconds for unauthorised stream status |
| `cacheSettings` | `streamNotFoundStatusCacheTimeout` | Cache timeout in seconds for not-found stream status |
| `dataStoreSettings` | `channelStorePath` | XML file path for channel definitions |
| `dataStoreSettings` | `groupStorePath` | XML file path for groups |
| `dataStoreSettings` | `playlistProviderStorePath` | XML file path for providers |

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
- `UrlFormat` (string): provider URL, optionally with a date placeholder
- `Country` (string, optional): provider country hint
- `ChannelNameOverride` (string, optional): force all channels from this provider to a fixed name

Date placeholder example in `UrlFormat`:

```text
https://example.com/playlists/{0:yyyy-MM-dd}.m3u
```

## Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Internet access (required at runtime to fetch source playlists)

All NuGet dependencies are restored automatically by `dotnet restore`.

### Build

```bash
dotnet build IptvPlaylistAggregator.slnx
```

### Run

```bash
dotnet run --project IptvPlaylistAggregator/IptvPlaylistAggregator.csproj
```

By default, the output playlist is written to `result.m3u` (configured in `appsettings.json`).

### Test

```bash
dotnet test IptvPlaylistAggregator.slnx
```

### Release

The repository includes `release.sh`, which delegates to the upstream deployment script used by the project maintainer.

```bash
bash ./release.sh 1.0.0
```

This script downloads and executes an external release helper from `https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`.

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

## Run as a Linux systemd service

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

## Project Structure

The solution contains the following projects:

- `IptvPlaylistAggregator`: main console application
- `IptvPlaylistAggregator.UnitTests`: unit tests

Key directories inside `IptvPlaylistAggregator/`:

| Directory | Purpose |
|-----------|---------|
| `Configuration/` | Settings classes for dependency injection |
| `Data/` | Sample data files (`channels.xml`, `groups.xml`, `providers.xml`) |
| `DataAccess/` | XML data objects and repository mapping |
| `Logging/` | Structured logging keys and operations |
| `Service/` | Aggregation, matching, fetch, and M3U build logic |
| `Service/Mapping/` | Extensions for mapping data objects to domain models |
| `Service/Models/` | Domain model classes |

### Dependencies

| Package | Purpose |
|---------|---------|
| `Microsoft.Extensions.Configuration` | Configuration reading infrastructure |
| `Microsoft.Extensions.Configuration.Binder` | Strongly-typed configuration binding |
| `Microsoft.Extensions.Configuration.Json` | JSON configuration provider |
| `Microsoft.Extensions.DependencyInjection` | Dependency injection container |
| `NuciDAL` | Data access layer utilities for XML repositories |
| `NuciExtensions` | General-purpose extension methods |
| `NuciLog` | Structured file and console logging |
| `NuciLog.Core` | Core logging abstractions |
| `NuciWeb.HTTP` | HTTP client utilities for fetching remote playlists |

## Contributing

Contributions are welcome. Please:
- Keep changes cross-platform
- Keep pull requests focused and consistent with the existing code style
- Update documentation when behaviour changes
- Add unit tests for any new or changed functionality

## Support

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or giving a ⭐️ on GitHub!

## Legal Notice

This software aggregates playlist sources. You are responsible for ensuring your usage complies with local laws and content licensing requirements.

## License

Licensed under the `GNU General Public License v3.0` or later.
See [LICENSE](./LICENSE) for details.