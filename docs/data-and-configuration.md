# Data And Configuration

## Configuration Loading

[Program](../IptvPlaylistAggregator/Program.cs) binds the following sections from [appsettings.json](../IptvPlaylistAggregator/appsettings.json):

| Section | Properties | Effect |
|---|---|---|
| `applicationSettings` | `OutputPlaylistPath` | Destination overwritten after a successful aggregation call. |
| `applicationSettings` | `DaysToCheck` | Number of prior dates considered for date-based provider cache fallback. The loop checks `1` through `DaysToCheck - 1`. |
| `applicationSettings` | `AreUnmatchedChannelsIncluded` | Enables the unmatched-channel pass. |
| `applicationSettings` | `AreTvGuideTagsEnabled` | Adds `tvg-*` and `group-title` output tags. |
| `applicationSettings` | `ArePlaylistDetailsTagsEnabled` | Adds provider ID and original provider channel name tags. |
| `cacheSettings` | `CacheDirectoryPath` | Root for cache files. Created during cache manager construction. |
| `cacheSettings` | Four stream timeout values | Expiration windows in seconds for `Alive`, `Dead`, `Unauthorised`, and `NotFound` entries loaded from disk. |
| `dataStoreSettings` | Three XML paths | Paths passed to the three `XmlRepository<T>` instances. |
| `nuciLoggerSettings` | Logger package settings | Controls logger destination and minimum level. |

Only the JSON provider is registered. No command-line or environment configuration override is composed. Relative paths are resolved by the current process working directory. The JSON and XML files are copied to build output by [IptvPlaylistAggregator.csproj](../IptvPlaylistAggregator/IptvPlaylistAggregator.csproj).

## XML Reference Data

The XML files are read as `NuciDAL` data objects and mapped into domain objects. Normal execution reads them; it does not write them back.

| Source | Data object | Required relationship |
|---|---|---|
| [channels.xml](../IptvPlaylistAggregator/Data/channels.xml) | `ChannelDefinitionDataObject` | `GroupId` must identify a loaded group because the aggregator indexes groups directly by ID. |
| [groups.xml](../IptvPlaylistAggregator/Data/groups.xml) | `GroupDataObject` | `Id`, `Name`, `Priority`, and `IsEnabled` define output grouping. |
| [providers.xml](../IptvPlaylistAggregator/Data/providers.xml) | `PlaylistProviderDataObject` | Enabled providers need a usable URL format and should use distinct priorities. |

Data-object defaults are significant:

- [ChannelDefinitionDataObject](../IptvPlaylistAggregator/DataAccess/DataObjects/ChannelDefinitionDataObject.cs) defaults `IsEnabled` to true and an absent group to `unknown`.
- [GroupDataObject](../IptvPlaylistAggregator/DataAccess/DataObjects/GroupDataObject.cs) defaults priority to `int.MaxValue`.
- [PlaylistProviderDataObject](../IptvPlaylistAggregator/DataAccess/DataObjects/PlaylistProviderDataObject.cs) defaults priority to `int.MaxValue` and caching to true. XML uses `AllowCaching` for that property.

The mapping extensions in [Service/Mapping](../IptvPlaylistAggregator/Service/Mapping/) preserve IDs, enabled flags, names, countries, groups, aliases, URLs, priorities, and provider metadata. The reverse mapping exists but is not used by the application workflow.

## Domain Records

| Type | Meaning |
|---|---|
| `Group` | Output group ID, display name, priority, and enabled state. |
| `ChannelDefinition` | Curated channel ID, `ChannelName`, country, group ID, logo, and enabled state. |
| `ChannelName` | Canonical name, optional country, and aliases used by matching. |
| `PlaylistProvider` | Provider identity, priority, cache policy, URL format, country override, and optional name override. |
| `Playlist` | Mutable list of `Channel` records. |
| `Channel` | Source URL plus display, group, logo, numbering, provider, and original-name metadata. |
| `MediaStreamStatus` | URL, `StreamState`, timestamp, and computed `IsAlive` property. |

## Cache Layers

[CacheManager](../IptvPlaylistAggregator/Service/CacheManager.cs) owns four process-local dictionaries:

| Cache | Key | Value | Use |
|---|---|---|---|
| Normalized names | Original name plus optional country prefix | Normalized string | Avoids repeating channel normalization. |
| Web downloads | URL | Response text or empty string | Avoids repeated downloads during one run. |
| Parsed playlists | Exact file content | `Playlist` | Avoids reparsing identical content. |
| Stream statuses | URL | Status and last-check time | Short-circuits media checks. |

`TryAdd` is used for all dictionary writes. In particular, the first status stored for a URL wins for the lifetime of the process; a later status for the same URL is not an update. The cache manager tests that describe repeated updates observe the first value because of this implementation detail.

## Persistent Cache Files

The cache directory contains:

- `{providerId}_playlist_{yyyy-MM-dd}.m3u`: optional provider playlist snapshots.
- `stream-statuses.csv`: URL, timestamp, and enum state separated by commas.

The stream-status CSV is loaded when `CacheManager` is constructed. Expiry is evaluated only during loading. `Unsupported` and `Blacklisted` entries have no timeout rule and remain loadable indefinitely. URLs containing commas are truncated before writing because the serializer does not quote or escape CSV fields. Malformed cache lines can fail cache initialization.

Provider snapshots are written only when caching is enabled and the current download parses into a non-empty playlist. Previous-date fallback reads snapshots only; it does not download or create missing historical files. The cache directory is intended for one process owner because there is no locking or atomic multi-process coordination.

## Persistent Outputs

- The aggregated M3U is written to `OutputPlaylistPath` by [Program](../IptvPlaylistAggregator/Program.cs).
- Logs are controlled by `nuciLoggerSettings`; URLs may appear in logs or generated artifacts because there is no dedicated redaction layer.
- XML reference files remain unchanged during a run.

Protect the configuration, output, cache, and log paths with filesystem permissions appropriate to any provider URLs that contain access tokens or other credentials.
