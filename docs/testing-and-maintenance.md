# Testing And Maintenance

## Test Projects

The solution contains one executable project and two test projects:

- [IptvPlaylistAggregator.UnitTests](../IptvPlaylistAggregator.UnitTests/): NUnit and Moq tests for deterministic service and model behavior. The project excludes itself from code coverage.
- [IptvPlaylistAggregator.IntegrationTests](../IptvPlaylistAggregator.IntegrationTests/): NUnit and Moq test sources for larger inputs, service interaction shapes, temporary cache construction, and parser/output combinations. Its project file is not marked with `IsTestProject`, so the current `dotnet test` command builds it but does not discover or execute these tests.

Both test projects reference the executable project and target `net10.0`. The main test dependencies are NUnit 4.6.1, NUnit3TestAdapter 6.3.0, Moq 4.20.72, and Microsoft.NET.Test.Sdk 18.10.1 where required by the project manifest. Only the unit-test project currently declares `IsTestProject=true` and the test SDK package.

## What Is Verified

| Area | Tests | Verified behavior |
|---|---|---|
| Name matching | [ChannelMatcherTests](../IptvPlaylistAggregator.UnitTests/Service/ChannelMatcherTests.cs), [ChannelMatcherIntegrationTests](../IptvPlaylistAggregator.IntegrationTests/ChannelMatcherIntegrationTests.cs) | Country-aware normalization, aliases, diacritics, provider naming variants, cache use, and collection-scale matching. |
| Media policy | [MediaSourceCheckerTests](../IptvPlaylistAggregator.UnitTests/Service/MediaSourceCheckerTests.cs), [MediaSourceCheckerIntegrationTests](../IptvPlaylistAggregator.IntegrationTests/MediaSourceCheckerIntegrationTests.cs) | Unsupported URL rejection, blacklist rejection, cached state interpretation, URL variety, and concurrent cached checks. |
| M3U handling | [PlaylistFileBuilderTests](../IptvPlaylistAggregator.UnitTests/Service/PlaylistFileBuilderTests.cs), [PlaylistFileBuilderIntegrationTests](../IptvPlaylistAggregator.IntegrationTests/PlaylistFileBuilderIntegrationTests.cs) | Parsing, malformed input handling, parser cache reuse, output tags, empty output, scale, and round trips. |
| Playlist model | [PlaylistTests](../IptvPlaylistAggregator.UnitTests/Service/PlaylistTests.cs) | Empty/null-or-empty semantics. |
| Provider fetching | [PlaylistFetcherIntegrationTests](../IptvPlaylistAggregator.IntegrationTests/PlaylistFetcherIntegrationTests.cs) | Task-based provider flow, metadata application shape, provider counts, URL forms, and configured date values using mocked downloader/parser/cache. |
| Aggregation | [PlaylistAggregatorIntegrationTests](../IptvPlaylistAggregator.IntegrationTests/PlaylistAggregatorIntegrationTests.cs) | Repository/service orchestration shape, output invocation, enabled/disabled fixture sizes, and large mocked inputs. |
| Cache construction | [CacheManagerIntegrationTests](../IptvPlaylistAggregator.IntegrationTests/CacheManagerIntegrationTests.cs) | In-memory status storage, concurrent writes, status varieties, temporary directory creation, and scale. |
| End-to-end-shaped flow | [EndToEndIntegrationTests](../IptvPlaylistAggregator.IntegrationTests/EndToEndIntegrationTests.cs) | Aggregator-scale scenarios with all collaborators mocked. |

The current local baseline is 245 passing unit tests and zero failures on .NET SDK 10.0.104. The integration-test project was also built explicitly, but no tests were discovered or executed from it. Treat 245 as an observed baseline, not as a permanent expected count: test additions change the count.

## Commands

Restore, build, and test the solution:

```bash
dotnet restore
dotnet build IptvPlaylistAggregator.slnx
dotnet test IptvPlaylistAggregator.slnx
```

The CI workflow in [.github/workflows/dotnet.yml](../.github/workflows/dotnet.yml) runs restore, build, and test on Ubuntu for pushes and pull requests targeting `master`. It uses .NET 10.x.

Run the application manually with:

```bash
dotnet run --project IptvPlaylistAggregator
```

A live run requires the copied JSON/XML files, writable output/cache/log paths, and outbound network access.

## Important Test Limitations

The test names and project layout should not be mistaken for full system integration:

- Repository tests use mocked `IFileRepository<T>` instances; checked-in XML deserialization is not exercised by the suite.
- Provider and media tests mock network-facing collaborators; real provider responses, HTTP status mapping, recursive playlist validation, and timeout behavior are not broadly exercised.
- The composition root, startup connectivity gate, top-level exception handling, output file write, and shutdown cache save lack a process-level test.
- Cache persistence tests do not fully verify CSV write/read round trips, expiry on reload, malformed lines, or comma-containing URLs.
- Provider duplicate-priority overwrite behavior, missing group IDs, and output metadata escaping are not directly asserted.
- Some scale-oriented tests assert only that output starts with `#EXTM3U`; they do not prove channel ordering, source selection, or tag contents.

When changing a boundary listed above, add a focused test before relying on existing green results.

## Change Guidance

### Changing Reference Data

Preserve IDs used by `GroupId`, provider IDs, and generated metadata. Keep enabled provider priorities unique. Validate that every channel definition references an existing group. Keep URL-bearing configuration and cache files protected.

### Changing Matching

Update focused cases in [ChannelMatcherTests](../IptvPlaylistAggregator.UnitTests/Service/ChannelMatcherTests.cs). Include country/no-country pairs, canonical names, aliases, normalization markers, and an explicit non-match. Avoid broadening matching without considering false positives.

### Changing Media Validation

Add tests for each new URL policy or `StreamState` mapping. Check cache-first behavior and recursive playlist behavior. Preserve the distinction between unsupported, blacklisted, HTTP-specific, and generic failure states unless the cache contract is revised too.

### Changing M3U Handling

Add both parser and serializer tests. Verify empty input, malformed ordering, metadata tags, URL preservation, and a representative provider dialect. Preserve the header, one-entry-per-channel, and URL-line conventions used by downstream clients.

### Changing Cache Formats

Treat `stream-statuses.csv` and dated provider files as compatibility formats. Add migration or backward-compatible loading before changing field order, timestamp format, enum names, file names, or URL escaping. Consider process concurrency and partial writes.

### Changing Concurrency

Recheck deterministic output order, shared cache behavior, exception aggregation, request volume, and synchronous waits inside parallel loops. Run the full suite because concurrency changes cross service boundaries.

## Documentation Maintenance

Update this directory when a change affects an ownership boundary, data contract, runtime branch, cache format, output format, test guarantee, or operational prerequisite. Keep [README](../README.md) for user-facing setup and [ARCHITECTURE.md](../ARCHITECTURE.md) for the concise architectural overview; link to the detailed document rather than duplicating all implementation notes.
