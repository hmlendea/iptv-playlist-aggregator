# Runtime Pipeline

## Scope

The application is a finite, single-process batch job. [Program](../IptvPlaylistAggregator/Program.cs) loads JSON configuration, creates a dependency-injection container, checks connectivity, invokes the aggregator, writes the generated M3U file, persists stream statuses, and logs shutdown. There is no web host, inbound listener, queue, database, or command-line argument protocol.

## Composition

[Service registrations](../IptvPlaylistAggregator/Service/ServiceCollectionExtensions.cs) register every application service as a singleton:

- `ICacheManager` -> `CacheManager`
- `IFileDownloader` -> `FileDownloader`
- `IPlaylistAggregator` -> `PlaylistAggregator`
- `IPlaylistFetcher` -> `PlaylistFetcher`
- `IPlaylistFileBuilder` -> `PlaylistFileBuilder`
- `IChannelMatcher` -> `ChannelMatcher`
- `IMediaSourceChecker` -> `MediaSourceChecker`
- Three `IFileRepository<T>` instances backed by `XmlRepository<T>`

Singleton lifetime is process-local. It allows the downloader, parser, matcher, media checker, and aggregator to share the same cache manager, but it does not provide cross-process coordination.

## Startup And Shutdown

1. `LoadConfiguration` registers `appsettings.json` as optional and reloadable.
2. Settings are bound once to `ApplicationSettings`, `CacheSettings`, and `DataStoreSettings`.
3. The logger and application services are added to the service provider.
4. A startup success event is logged.
5. `NetworkUtils.HasInternetAccess()` is checked. A negative result logs a fatal startup failure and returns immediately.
6. `IPlaylistAggregator.GatherPlaylist()` produces M3U text.
7. The text is written to `ApplicationSettings.OutputPlaylistPath`.
8. `AggregateException` is recursively expanded into fatal log entries. Other exceptions are logged once as unknown fatal failures.
9. Stream statuses are written by `ICacheManager.SaveCacheToDisk()`.
10. A shutdown success event is logged.

The connectivity early return bypasses cache persistence and shutdown logging. Aggregation or output exceptions are caught, after which cache persistence is still attempted. The process does not define an explicit exit-code contract for partial provider failures or handled aggregation exceptions.

## Aggregation Flow

[PlaylistAggregator](../IptvPlaylistAggregator/Service/PlaylistAggregator.cs) owns the ordering and selection workflow:

1. Read groups, channel definitions, and providers from XML repositories.
2. Map data objects to domain models.
3. Index groups by ID.
4. Sort channel definitions by group priority and then definition name.
5. Keep enabled providers.
6. Fetch provider playlists through `IPlaylistFetcher`.
7. Flatten all provider channels.
8. Remove blank URLs and de-duplicate by exact URL, keeping the first occurrence.
9. Keep channel definitions whose own `IsEnabled` flag and referenced group's `IsEnabled` flag are both true.
10. For each enabled definition, find matching provider channels and select the first playable URL.
11. Resolve the selected source with curated ID, name, country, group, logo, provider ID, and original provider channel name.
12. Optionally find, probe, and append unmatched provider channels.
13. Assign sequential one-based channel numbers.
14. Serialize the final `Playlist` through `IPlaylistFileBuilder`.

The final output order is based on the sorted original definitions, not on the completion order of parallel matching. Unmatched channels are appended after curated channels, grouped by exact provider name, and alphabetized by that name.

## Concurrency

Provider fetches are started as tasks and joined with `Task.WaitAll`. Channel matching and unmatched-channel checks use `Parallel.ForEach`; each playable check is awaited synchronously through `.Result` inside the loop. `ConcurrentBag<Channel>` collects parallel results, while the final definition-order pass restores deterministic curated ordering.

`CacheManager` uses concurrent dictionaries for normalized names, stream statuses, downloaded text, and parsed playlists. The code does not set an explicit maximum degree of parallelism for provider requests or channel checks. A run can therefore create substantial outbound traffic against remote providers and media endpoints.

## Failure Boundaries

- Empty or failed provider content becomes an omitted provider playlist.
- A date-based provider may fall back to cached previous dates.
- Invalid M3U input becomes `null` through `TryParseFile` and is treated as unusable.
- A channel definition with no playable matching source is omitted.
- Media failures become cached `StreamState` values rather than terminating the aggregation.
- Missing output-directory permissions or other output write failures reach the top-level exception handler.
- Repository, mapping, configuration, and cache initialization failures can reach the top-level exception boundary before normal cache saving.

## Ownership Boundaries

- `Program` owns process lifecycle, configuration binding, output writing, and top-level logging.
- `PlaylistAggregator` owns cross-service sequencing and final channel ordering.
- `PlaylistFetcher` owns provider retrieval, provider metadata application, and date-cache fallback.
- `ChannelMatcher` owns name normalization and alias comparison.
- `MediaSourceChecker` owns URL policy and playability classification.
- `PlaylistFileBuilder` owns M3U parsing and serialization.
- `CacheManager` owns process caches and cache files.
- XML repositories own reference-data loading; mapping extensions own data-object/domain conversion.

No service owns a transaction spanning the input XML, remote requests, cache files, and output file.
