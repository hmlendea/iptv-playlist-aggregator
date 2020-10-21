[![Build Status](https://travis-ci.com/hmlendea/iptv-playlist-aggregator.svg?branch=master)](https://travis-ci.com/hmlendea/iptv-playlist-aggregator)

[![Support this on Patreon](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_patreon.png)](https://www.patreon.com/hmlendea) [![Donate through PayPal](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_paypal.png)](https://www.paypal.com/donate?hosted_button_id=6YVRGJHDGWGKQ)

# About

IPTV Playlist Aggregator is a tool for downloading IPTV playlists from multiple sources and aggregating their channels into a single playlist. It will match the duplicated channels into a single one, based on their name, and will override any channel data (such as logo, TVG ID, etc) with your own custom one.

Example use case:
Run as a background service on a Raspberry Pi, to periodically output the playlist to an HTTP server, from where it can be directly accessed by the IPTV application via URL.

# Contributions

Feel free to fork and implement any change you consider useful, and open a pull request so that I can review it and merge into the master branch here.

Rules:
 - Make sure that anything you implement workson any OS supported by .NET Core
 - Make sure that your code is formatted correctly and it respects the basic C# coding and naming standards

# Instructions

## Compiling

Firstly you need to make sure you have the .NET Core SDK installed and up to date.

Chose one of the following methods based on which system you want this service to be running on, and run the command inside of the source directory (where the .csproj is)

### For the current system:

`dotnet publish -c Release`

The output will be located in `./bin/Release/netcoreapp2.2`

### For a different system:

`dotnet publish -c Release -r [RID]`

For a list of all possible RID values, check out [the official documentation](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog).

The output will be located in `./bin/Release/netcoreapp2.2/[RID]/'.

If the target system will have the *.NET Core Runtime* installed, delete or ignore the `./bin/Release/netcoreapp2.2/[RID]/publish` directory.
If not, use **only** the publish directory, since that one contains all the necessary libraries that the runtime would normally provide.

## Running in background as a service

**Note:** The following instructions only apply for *Linux* distributions using *systemd*.

Create the following service file: /lib/systemd/system/iptv-playlist-aggregator.service
```
[Unit]
Description=IPTV Playlist Aggregator

[Service]
WorkingDirectory=[ABSOLUTE_PATH_TO_SERVICE_DIRECTORY]
ExecStart=[ABSOLUTE_PATH_TO_SERVICE_DIRECTORY]/IptvPlaylistAggregator
User=[YOUR_USERNAME]

[Install]
WantedBy=multi-user.target
```

Create the following timer file: /lib/systemd/system/iptv-playlist-aggregator.timer
```
[Unit]
Description=Periodically aggregates an IPTV M3U playlist

[Timer]
OnBootSec=5min
OnUnitActiveSec=50min

[Install]
WantedBy=timers.target
```

Values that you might want to change:
 - *OnBootSec*: the delay before the service is started after the OS is booted
 - *OnUnitActiveSec*: how often the service will be triggered

In the above example, the service will start 5 minutes after boot, and then again once every 50 minutes.

## Configuration

### Settings

The settings are stored in the `appsettings.json` file in the root directory.

 - *channelStorePath*: The file where all your channel data is stored
 - *groupStorePath*: The file where all your group data is stored
 - *playlistProviderStorePath*: The file where all the playlist provider URLs are stored
 - *outputPlaylistPath*: The location where the output playlist will be written. Can be used to write directly to an http server
 - *cacheDirectoryPath*: The directory where all the cache files will be written. Leave as default unless you specifically require to move it
 - *daysToCheck*: How far in the past to go for each playlist. If today's playlist is not found (sometimes the providers skip some days) then the service will move on to the previous day, again and again until one is found or the daysToCheck limit is reached.
 - *canIncludeUnmatchedChannels*: Boolean value indicating whether provider channels that were not able to be matched with the data in the channelStorePath file should be included in the output file (in the Unknown category) or not at all
 - *areTvGuideTagsEnabled*: Boolean value indicating whether TV Guide tags (logo URLs, groups, TVG IDs, channel numbers, etc) should be included in the output file or not
 - *arePlaylistDetailsTagsEnabled*: Boolean value indicating whether playlist details (playlist ID, the playlist's original channel name) should be included in the output file or not
 - *userAgent*: String value indicating the UserAgent that should be used when performing HTTP operations

### Channel data

The channel data file is an XML file, whose name and location is configred in `appsetting.json` by the *channelStorePath* value.

The file needs to be .NET serializable as an array of ChannelDefinitionEntity objects.

ChannelDefinitionEntity fields:
 - *Id* (string): The TVG ID. If using a TVG provider within your IPTV application, make sure the channel IDs match the TVG IDs of your provider.
 - *IsEnabled* (bool): Indicates whether the final playlist will contain this channel or not. Even if enabled, if the group is disabled, the channel will still be omitted.
 - *Name* (string): The name of the channel, as displayed in your IPTV application.
 - *Country* (string): (Optional) The country where the channel is being broadcasted. The `tvg-country` property will be populated with this value, if it exists. It will also be used uin the channel matching process.
 - *GroupId* (string): The ID of the group that this channel will be part of.
 - *LogoUrl* (string): The URL to a logo for the channel. Make sure your IPTV application supports the logo format you provide here.
 - *Aliases* (string collection): Different variants of the name of the channel, as it can appear in the provider playlists. This is the criteria used to match provider channels to this definition.

### Group data

The group data file is an XML file, whose name and location is configred in `appsetting.json` by the *groupStorePath* value.

The file needs to be .NET serializable as an array of GroupEntity objects.

GroupEntity fields:
 - *Id* (string): Used for matching channels to this group
 - *IsEnabled* (bool): Indicates whether the final playlist will contain channels in this group or not
 - *Priority* (int): The order of the group, starting from 1. The lowest value means that the group will appear first in your IPTV application. The playlist will also have its channels sorted based on their group's priority
 - *Name* (string): The name of the group, as displayed in your IPTV application.

### Providers data

The providers data file is an XML file, whose name and location is configred in `appsetting.json` by the *playlistProviderStorePath* value.

The file needs to be .NET serializable as an array of PlaylistProviderDefinitionEntity objects.

PlaylistProviderDefinitionEntity fields:
 - *Id* (string): The ID of the provider. You can put anything here, used only to distinguish between them.
 - *IsEnabled* (bool): Indicates whether this provider will be used or not.
 - *Priority* (int): The lower the value, the sooner the provider will be processed. Try to make sure the most reliable providers are processed first, as once a channel is matched with a provider, it will be ignored for all other providers after it.
 - *AllowCaching* (bool): (Optional) Indicates whether this provider's playlist should be cached or not. Useful when the provider updates the playlist multiple times a day. By default it's true.
 - *UrlFormat* (string): The URL to the m3u playlist file of that provider. Replace the date part of the URL with a timestamp format. For example, *2019-05-19* will be replaced with *{0:yyyy-MM-dd}*. The *0* is the calendar day that is processed (today, or one of the previous ones depending on the *daysToCheck* setting)
 - *Country* (string): (Optional) If set, the country will be used in the channel matching process.
 - *ChannelNameOverride* (string): (Optional) The channel name override for all the channels in the provider's playlist.

[![Support this on Patreon](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_patreon.png)](https://www.patreon.com/hmlendea) [![Donate through PayPal](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_paypal.png)](https://www.paypal.com/donate?hosted_button_id=6YVRGJHDGWGKQ)
