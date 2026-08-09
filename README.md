[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/iptv-playlist-aggregator)](https://github.com/hmlendea/iptv-playlist-aggregator/releases/latest)
[![Build Status](https://github.com/hmlendea/iptv-playlist-aggregator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/iptv-playlist-aggregator/actions/workflows/dotnet.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://gnu.org/licenses/gpl-3.0)

# IPTV Playlist Aggregator

IPTV Playlist Aggregator is a .NET console application that retrieves playlists from multiple providers and aggregates them into one curated M3U output.

## 📑 Table of Contents

- [Capabilities](#-capabilities)
- [Usage](#-usage)
- [Known Limitations](#-known-limitations)
- [System Requirements](#-system-requirements)
- [Installation](#-installation)
  - [CLI Installation](#cli-installation)
- [Configuration](#-configuration)
- [Development](#-development)
  - [Requirements](#requirements)
  - [Setup](#setup)
  - [Build](#build)
  - [Run](#run)
  - [Test](#test)
  - [Release](#release)
  - [Dependencies](#dependencies)
- [Project Structure](#-project-structure)
- [Contributing](#-contributing)
- [Supporting the Project](#-supporting-the-project)
- [License](#-license)

## ✨ Capabilities

- Retrieves M3U playlists from multiple configurable providers.
- Matches provider channels against configured channel definitions and aliases.

## 🚀 Usage

```bash
dotnet run --project IptvPlaylistAggregator
```

Before running, adjust data files in `IptvPlaylistAggregator/Data/` and revise settings in `IptvPlaylistAggregator/appsettings.json`.

## ⚠️ Known Limitations

- Playlist retrieval and stream validation require internet access.

## 🖥️ System Requirements

- **OS:** Linux, macOS, Windows.
- **RAM:** 256 MB minimum.
- .NET 10.0 runtime.

## 📦 Installation

[![Obtain it from GitHub](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/github.png)](https://github.com/hmlendea/iptv-playlist-aggregator/releases)

### CLI Installation

Download the archive for your platform from the latest GitHub release and execute the extracted binary.

## ⚙️ Configuration

All settings are loaded from the configuration file. The subsequent keys are recognised:

| Section | Key | Description |
|---------|-----|-------------|
| `nuciLoggerSettings` | `logFilePath` | Path for the application log file. |
| `nuciLoggerSettings` | `isFileOutputEnabled` | Enables or disables file logging. |
| `nuciLoggerSettings` | `minimumLevel` | Minimum severity level written to logs. |
| `applicationSettings` | `outputPlaylistPath` | Output path for the aggregated M3U playlist. |
| `applicationSettings` | `daysToCheck` | Number of days checked for date-based provider URLs. |
| `applicationSettings` | `areUnmatchedChannelsIncluded` | Includes unmatched channels in the output when enabled. |
| `applicationSettings` | `areTvGuideTagsEnabled` | Enables TV guide tags in output entries. |
| `applicationSettings` | `arePlaylistDetailsTagsEnabled` | Enables source playlist detail tags in output entries. |
| `cacheSettings` | `cacheDirectoryPath` | Directory used for cached data. |
| `cacheSettings` | `streamAliveStatusCacheTimeout` | Cache timeout in seconds for alive stream checks. |
| `cacheSettings` | `streamDeadStatusCacheTimeout` | Cache timeout in seconds for dead stream checks. |
| `cacheSettings` | `streamUnauthorisedStatusCacheTimeout` | Cache timeout in seconds for unauthorised stream checks. |
| `cacheSettings` | `streamNotFoundStatusCacheTimeout` | Cache timeout in seconds for not-found stream checks. |
| `dataStoreSettings` | `channelStorePath` | XML path for channel definitions. |
| `dataStoreSettings` | `groupStorePath` | XML path for group definitions. |
| `dataStoreSettings` | `playlistProviderStorePath` | XML path for playlist provider definitions. |

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Setup

All NuGet dependencies are restored automatically by `dotnet restore`.

### Build

```bash
dotnet build IptvPlaylistAggregator
```

### Run

```bash
dotnet run --project IptvPlaylistAggregator
```

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

### Dependencies

| Package | Purpose |
|---------|---------|
| `Microsoft.Extensions.Configuration` | Configuration loading infrastructure. |
| `Microsoft.Extensions.Configuration.Binder` | Strongly typed configuration binding. |
| `Microsoft.Extensions.Configuration.Json` | JSON configuration provider. |
| `Microsoft.Extensions.DependencyInjection` | Dependency injection container. |
| `NuciDAL` | XML data access primitives. |
| `NuciExtensions` | General utility extensions. |
| `NuciLog` | Logging implementation. |
| `NuciLog.Core` | Core logging abstractions. |
| `NuciWeb.HTTP` | HTTP retrieval utilities for remote playlists. |

## 🗂️ Project Structure

The solution contains the subsequent projects:

- `IptvPlaylistAggregator`: Main console application.
- `IptvPlaylistAggregator.UnitTests`: Unit test suite.

The key directories inside `IptvPlaylistAggregator/` are:

| Directory | Purpose |
|-----------|---------|
| `Configuration/` | Application, cache, and datastore settings models. |
| `Data/` | XML source files for channels, groups, and providers. |
| `DataAccess/` | Data objects used for XML persistence. |
| `Logging/` | Logging operation and contextual keys. |
| `Service/` | Aggregation, validation, matching, and output generation services. |

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Maintain the pull requests as focused and consistent with the existing code style
- Revise the documentation when behaviour changes
- Properly test all changes, including edge cases and error conditions
- Add unit tests for any new or changed functionality

## 💝 Supporting the Project

Discovered a problem or have a suggestion? [Open an issue](https://github.com/hmlendea/iptv-playlist-aggregator/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0` or later.
See [LICENSE](./LICENSE) for further information.