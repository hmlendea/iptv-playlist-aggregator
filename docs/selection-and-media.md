# Selection And Media Validation

## Provider Ordering

[PlaylistFetcher](../IptvPlaylistAggregator/Service/PlaylistFetcher.cs) starts one fetch task per provider. It ignores `null` or empty playlists, inserts successful playlists into a `ConcurrentDictionary<int, Playlist>` keyed by `Priority`, then returns values in ascending priority order.

The dictionary update function replaces the existing value when priorities collide. Consequently, duplicate priorities are not a merge: the later provider result processed by the task-result loop replaces the earlier playlist. The checked-in provider XML should use unique priorities even though the data model does not enforce uniqueness.

For every parsed channel, the fetcher sets `PlaylistId` to the provider ID. A non-empty provider `Country` overwrites the channel country. A non-empty `ChannelNameOverride` overwrites the channel name while preserving the original name in `PlaylistChannelName` when it was already parsed.

## Candidate Preparation

[PlaylistAggregator](../IptvPlaylistAggregator/Service/PlaylistAggregator.cs) flattens the ordered provider playlists and applies two filters:

1. Channels with null, empty, or whitespace URLs are removed.
2. Channels are de-duplicated by exact URL with `DistinctBy`, keeping the first occurrence in the merged provider order.

This means matching sees one candidate per exact URL, but different URLs from the same or different providers remain separate candidates.

## Channel Matching

[ChannelMatcher](../IptvPlaylistAggregator/Service/ChannelMatcher.cs) first checks exact string equality. If that fails, it normalizes both names, including their country values, and compares the resulting strings. A curated definition matches when either its canonical name or any alias matches the provider channel name.

Normalization is intentionally domain-specific:

1. Prefix the name with `country + ": "` when a country is supplied.
2. Reuse the normalized-name cache when possible.
3. Remove diacritics.
4. Remove known substrings such as `iptvsource.com` and `backup`.
5. Apply compiled replacements for country formats, quality/resolution markers, schedule markers, language forms, and common spelling forms.
6. Convert to uppercase.
7. Keep only ASCII letters and digits.

Country is part of the normalized comparison when present. A provider country is therefore capable of preventing an otherwise similar name from matching a definition. Matching is not fuzzy: there is no edit distance or token similarity calculation.

## Curated Channel Selection

For each enabled definition, the aggregator collects all candidate channels where the matcher returns true. It then checks candidates in the filtered provider order and selects the first URL for which `IsSourcePlayableAsync` returns true.

The selected output channel receives:

- the definition ID and canonical display name;
- the definition country, group name, and logo;
- the selected provider ID, original provider channel name, and URL.

If no candidate matches, or every matching URL is non-playable, the definition is omitted. Definitions are evaluated in parallel, but the final output is reconstructed in the previously sorted definition order: group priority first, then canonical name.

Only definitions with both `IsEnabled == true` and an enabled referenced group participate. A missing group ID is not treated as an empty group; the group dictionary is indexed directly and can cause the aggregation to fail.

## Unmatched Channels

When `AreUnmatchedChannelsIncluded` is false, this phase returns no channels. When enabled, it:

1. Keeps provider channels that match no channel definition, including disabled definitions.
2. Groups them by exact provider `Name` and keeps the first channel per name.
3. Orders the representatives alphabetically by exact name.
4. Probes each representative URL in parallel.
5. Keeps only playable representatives.
6. Appends them after all curated channels.

The unmatched pass does not resolve a provider channel into curated metadata. It retains the provider channel's existing fields.

## Media State Machine

[MediaSourceChecker](../IptvPlaylistAggregator/Service/MediaSourceChecker.cs) checks the stream-status cache first. Any cached state is authoritative for the current process, and only `Alive` is playable. A missing cached state follows this decision tree:

| Condition | Result |
|---|---|
| YouTube video URL, TinyURL, non-HTTP scheme, or `.mp4` URL | `Unsupported` |
| URL contains the configured blacklisted source | `Blacklisted` |
| URL contains `.m3u` or `.m3u8` | Playlist validation |
| Any other URL | Direct stream validation |

Direct validation performs an HTTP GET and maps status codes as follows: `200 OK` -> `Alive`, `401 Unauthorized` -> `Unauthorised`, `404 NotFound` -> `NotFound`, and every other status or caught failure -> `Dead`.

Playlist validation first checks the playlist URL. A URL containing `googlevideo` is treated as alive after the initial stream check. Otherwise, the playlist text is downloaded and parsed. An empty or invalid playlist is `Dead`. Each parsed entry is recursively checked until one is playable. Relative entries are attempted relative to the playlist directory and the URL host root. If none succeeds, the playlist is `Dead`.

Every newly computed state is stored with `DateTime.UtcNow`. The media checker also stores playlist response text in the web-download cache during its status request. Exceptions from the downloader are suppressed at the downloader boundary; HTTP failures therefore normally degrade to `Dead` rather than aborting the run.

## Stream States

`StreamState` values are:

- `Alive`: accepted as a source.
- `Dead`: request or playlist validation failed without a more specific status.
- `Unauthorised`: HTTP 401.
- `NotFound`: HTTP 404.
- `Unsupported`: known incompatible URL form.
- `Blacklisted`: explicitly rejected source.

All non-`Alive` states are rejected by channel selection. Their persistence and expiry behavior is described in [Data And Configuration](data-and-configuration.md).
