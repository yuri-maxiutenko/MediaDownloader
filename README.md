The Media Downloader Project
![Media Downloader logo](MediaDownloader/Images/icon.png)
====================
Just a small user-friendly UI wrapper (C#/WPF, .NET) over the great video downloading command-line utility [yt-dlp](https://github.com/yt-dlp/yt-dlp) which allows to download videos in different quality from various internet resources (YouTube, Vimeo, Facebook, you name it). Downloading playlists is supported too.

## Main Features

**Media Downloader** is capable of downloading almost any video from all major providers (YouTube, Vimeo, Facebook, etc.). Just give it a link and voilà! Links to playlists are fine too.

Currently the application supports the following download formats:

* Best quality video
* Best quality MP4 video
* Best quality video available by direct link
* Audio only

Also, **Media Downloader** supports download history and stores the list of recently used folders.

Comes with an installer, which includes everything necessary: the application itself, [yt-dlp](https://github.com/yt-dlp/yt-dlp) and the [FFmpeg](https://ffmpeg.org/) converter. yt-dlp keeps itself up to date automatically on application start.

Currently **Media Downloader** is localized to English and Russian. The application language is automatically selected depending on current Windows locale.

## Installation

Download the MSI installer from the [Releases](https://github.com/yuri-maxiutenko/MediaDownloader/releases) page and run it. The installation is per-user (to `%LOCALAPPDATA%\Programs\Wolfcub\Media Downloader`) and does not require administrator permissions.

A portable ZIP build is also attached to each release — unpack it anywhere and run `MediaDownloader.exe`.

## Requirements

The application targets **.NET 10** and ships **self-contained**, so no .NET runtime needs to be installed. The bundled **yt-dlp** may additionally require the [Microsoft Visual C++ Redistributable](https://github.com/yt-dlp/yt-dlp#dependencies) on systems where it is not already present.

## Screenshots

![Main window](Screenshots/main-window.png?raw=true)

## Building from Source

Prerequisites: [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) or later. Everything else (including the WiX toolset for the installer) restores from NuGet.

```powershell
# Build the application (x64 only)
dotnet build MediaDownloader.sln -c Release -p:Platform=x64

# Run the tests
dotnet test MediaDownloader.Tests/MediaDownloader.Tests.csproj -c Release -p:Platform=x64

# Build the MSI installer (publishes the app and produces out/MediaDownloaderSetup.msi)
dotnet build Installers/MediaDownloaderSetup/MediaDownloaderSetup.wixproj -c Release -p:Platform=x64
```

## Third-Party Software

The installer redistributes the following tools from the `third-party` folder:

* [yt-dlp](https://github.com/yt-dlp/yt-dlp) — released into the public domain under [The Unlicense](https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE)
* [FFmpeg](https://ffmpeg.org/) — licensed under the [GNU GPL/LGPL](https://ffmpeg.org/legal.html); see `third-party/ffmpeg/LICENSE.txt` for the license of the bundled build

## License

[MIT](LICENSE)
