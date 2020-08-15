The Media Downloader Project
====================
Just a small user-friendly UI wrapper (C#/WPF, .NET Core) over the great video downloading command-line utility [youtube-dl](https://github.com/ytdl-org/youtube-dl) which allows to download videos in different quality from various internet resources (YouTube, Vimeo, Facebook, you name it). Downloading playlists is supported too.

## Main Features

**Media Downloader** is capable of downloading almost any video from all major providers (YouTube, Vimeo, Facebook, etc.). Just give it a link and voilà! Links to playlists are fine too.

Currently the application supports the following download formats:

* best video quality;
* best video quality available via direct link;
* audio only.

Also, **Media Downloader** supports download history and stores the list of recently used folders.

Comes with an installer, which includes everything necessary: the application itself, **youtube-dl** and the [FFmpeg](https://ffmpeg.org/) converter.

The application is installed to current user's AppData folder and doesn't require administrator's permissions.

Curently **Media Downloader** is localized to English and Russian. The application language is automatically selected depending on current Windows locale.

## Download

The latest build could be found here: [https://bitbucket.org/wolfcub/youtubedownloader/downloads](https://bitbucket.org/wolfcub/youtubedownloader/downloads/)

## Requirements

**.NET Framework 4.7.2 or higher**

## Screenshots

Video download in progress:
![Video download in progress](https://bitbucket.org/wolfcub/mediadownloader/raw/440ff89ea572b69cf221473e9c0a1087ce483b6a/Screenshots/Annotation%202020-06-29%20210558.png)

Video download complete:
![Video download complete](https://bitbucket.org/wolfcub/mediadownloader/raw/440ff89ea572b69cf221473e9c0a1087ce483b6a/Screenshots/Annotation%202020-06-29%20210909.png)
