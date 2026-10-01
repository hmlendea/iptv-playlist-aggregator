# Playlist Format

## Input Parser

[PlaylistFileBuilder](../IptvPlaylistAggregator/Service/PlaylistFileBuilder.cs) parses line-oriented M3U text. `TryParseFile` returns `null` for null, empty, whitespace-only, or any input that throws during parsing. `ParseFile` exposes the underlying exception behavior.

Parsing rules:

- Lines beginning with `#EXTINF` create a channel and use the text after the last comma as both `Name` and `PlaylistChannelName`.
- Lines beginning with `#EXT-X-STREAM-INF` create a channel with a null name.
- The next non-comment line becomes the current channel URL.
- Other lines beginning with `#` are ignored.
- A URL before any entry header throws `InvalidOperationException`.
- The parser does not validate that the first line is `#EXTM3U`.
- Parsed playlists are cached by exact input content.

A line-oriented parser means provider dialects outside these header and URL conventions need focused compatibility tests before parser changes are made.

## Output Shape

`BuildFile` always emits `#EXTM3U` followed by the platform newline. Each channel then emits:

```text
#EXTINF:-1[,optional tags],Channel Name
https://source.example/stream
```

The builder preserves the supplied channel order and URL. It does not escape quotes or commas in metadata values. Channel numbers and other metadata are supplied by the aggregator or caller; the builder does not calculate them.

## Optional Tags

When `AreTvGuideTagsEnabled` is true, the `#EXTINF` line includes:

- `tvg-chno` from `Channel.Number`;
- `tvg-id` from `Channel.Id`;
- `tvg-name` from `Channel.Name`;
- `tvg-logo` when `LogoUrl` is non-empty;
- `tvg-country` when `Country` is non-empty;
- `group-title` when `Group` is non-empty.

When `ArePlaylistDetailsTagsEnabled` is true, it includes:

- `playlist-id` from `PlaylistId`;
- `playlist-channel-name` from `PlaylistChannelName`.

Tags are independent. Both can be enabled, either can be disabled, and missing optional values suppress only the corresponding conditional tag. Values are interpolated directly into quoted attributes, so configuration and source data containing quotes can produce malformed metadata.

## Output Invariants

- An empty playlist serializes to exactly the M3U header and a trailing newline.
- The aggregator assigns channel numbers sequentially starting at one after filtering and ordering.
- Curated channels precede unmatched channels.
- The process writes the generated text to `ApplicationSettings.OutputPlaylistPath`, overwriting that path on each successful write.

## Compatibility Evidence

[PlaylistFileBuilderTests](../IptvPlaylistAggregator.UnitTests/Service/PlaylistFileBuilderTests.cs) cover null/empty input, malformed ordering, single and multiple entries, comments, `EXT-X-STREAM-INF`, cache reuse, output tags, and basic serialization. [PlaylistFileBuilderIntegrationTests](../IptvPlaylistAggregator.IntegrationTests/PlaylistFileBuilderIntegrationTests.cs) cover generated and parsed playlists up to 1,000 channels and a build/parse round trip.
