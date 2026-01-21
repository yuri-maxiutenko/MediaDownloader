
<div align="right">
  <details>
    <summary >🌐 Language</summary>
    <div>
      <div align="center">
        <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=en">English</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=zh-CN">简体中文</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=zh-TW">繁體中文</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=ja">日本語</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=ko">한국어</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=hi">हिन्दी</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=th">ไทย</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=fr">Français</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=de">Deutsch</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=es">Español</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=it">Italiano</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=ru">Русский</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=pt">Português</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=nl">Nederlands</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=pl">Polski</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=ar">العربية</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=fa">فارسی</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=tr">Türkçe</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=vi">Tiếng Việt</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=id">Bahasa Indonesia</a>
        | <a href="https://openaitx.github.io/view.html?user=yuri-maxiutenko&project=MediaDownloader&lang=as">অসমীয়া</
      </div>
    </div>
  </details>
</div>

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

Comes with an installer, which includes everything necessary: the application itself, [yt-dlp](https://github.com/yt-dlp/yt-dlp) and the [FFmpeg](https://ffmpeg.org/) converter.

The application is installed to current user's AppData folder and doesn't require administrator's permissions.

Curently **Media Downloader** is localized to English and Russian. The application language is automatically selected depending on current Windows locale.

## Requirements

Starting from version **2.1**, the application uses **[.NET 7 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)**. But you shouldn't need to install it, as Media Downloader is provided as **[a self-contained application](https://devblogs.microsoft.com/dotnet/app-trimming-in-net-5/)**.

Also, **yt-dlp** requires **[Microsoft Visual C++ 2010 Redistributable Package (x86)](https://www.microsoft.com/en-us/download/details.aspx?id=5555)**.

## Screenshots

Video download in progress:
![Video download in progress](https://github.com/yuri-maxiutenko/MediaDownloader/blob/master/Screenshots/Annotation%202020-06-29%20210558.png?raw=true)

Video download complete:
![Video download complete](https://github.com/yuri-maxiutenko/MediaDownloader/blob/master/Screenshots/Annotation%202020-06-29%20210909.png?raw=true)
