# Repository Documentation

This directory is the implementation-grounded knowledge base for IPTV Playlist Aggregator. It complements the user-facing [README](../README.md) and the shorter architectural overview in [ARCHITECTURE.md](../ARCHITECTURE.md).

## Documents

- [Runtime Pipeline](runtime.md): process startup, dependency injection, aggregation flow, concurrency, and failure boundaries.
- [Data And Configuration](data-and-configuration.md): settings, XML reference data, domain mappings, cache state, and filesystem artefacts.
- [Selection And Media Validation](selection-and-media.md): channel matching, provider precedence, source probing, and stream states.
- [Playlist Format](playlist-format.md): M3U parsing, output serialization, metadata tags, and compatibility rules.
- [Testing And Maintenance](testing-and-maintenance.md): test structure, verified behavior, coverage gaps, and change guidance.

## Repository Entry Points

- [Program](../IptvPlaylistAggregator/Program.cs): composition root and process lifecycle.
- [Service registrations](../IptvPlaylistAggregator/Service/ServiceCollectionExtensions.cs): singleton service and XML repository registrations.
- [Application services](../IptvPlaylistAggregator/Service/): orchestration, fetching, matching, validation, caching, and M3U handling.
- [Domain models](../IptvPlaylistAggregator/Service/Models/): in-memory records exchanged between services.
- [Reference data](../IptvPlaylistAggregator/Data/): curated channels, groups, and providers.
- [Unit tests](../IptvPlaylistAggregator.UnitTests/Service/): deterministic service and model tests.
- [Integration tests](../IptvPlaylistAggregator.IntegrationTests/): service-scale tests using mocks and temporary cache state.

## Verified Baseline

The repository baseline recorded in the local engineering notes is 245 passing tests and zero failures on .NET SDK 10.0.104. Re-run the suite after implementation changes:

```bash
dotnet test IptvPlaylistAggregator.slnx
```

The test projects target `net10.0`. The executable also requires outbound network access when run because [Program](../IptvPlaylistAggregator/Program.cs) performs a startup connectivity check and the pipeline retrieves provider and media URLs.
