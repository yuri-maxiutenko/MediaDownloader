# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Calendar Versioning](https://calver.org/) (`YYYY.MM.DD`, the UTC release date).
Versions before 2026 used Semantic Versioning.

## [Unreleased]

### Added
- Bundled QuickJS JavaScript runtime (~0.7 MB in the installer). yt-dlp requires one to solve YouTube's JavaScript challenges; without it YouTube extraction is deprecated and some formats are missing. A Deno or Node.js 22+ installation already on the machine is preferred automatically, with the bundled runtime as the guaranteed fallback.
- Unit test project (`MediaDownloader.Tests`) covering the yt-dlp output parser, JSON mapping, file-name sanitization, argument construction, and storage.
- CI workflow running build + tests on every push and pull request.
- Release workflow that publishes the MSI, a portable ZIP and auto-generated notes as soon as the `dev` -> `master` release PR is merged.
- Dependabot configuration for NuGet packages and GitHub Actions.
- `.editorconfig` and `Directory.Build.props` for consistent code style and shared build settings.

### Changed
- Upgraded to **.NET 10** with nullable reference types enabled solution-wide (zero-warning build).
- Migrated JSON parsing from Newtonsoft.Json to System.Text.Json.
- Replaced deprecated `Microsoft.Toolkit.Mvvm` with `CommunityToolkit.Mvvm` 8 using source-generated observable properties and commands.
- Rebuilt application composition around the .NET Generic Host: dependency injection, options-validated configuration, and hosted database initialization.
- Moved download orchestration into the `MediaDownloader.Download` library; the view model no longer performs I/O or object construction.
- Storage layer is now asynchronous and disposed properly; EF Core migration added for non-nullable columns.
- Replaced the WinForms folder browser with the native WPF `OpenFolderDialog` (drops the WinForms dependency).
- Migrated the installer from WiX 3 (heat.exe harvesting) to SDK-style WiX 5 — the MSI now builds with plain `dotnet build` on any machine.
- Regexes are now compile-time source-generated instead of being rebuilt for every yt-dlp output line.
- Updated the bundled yt-dlp (2023.03.04 → 2026.07.04) and FFmpeg (2023-04-19 essentials build → 8.1.2 essentials build, gyan.dev).
- yt-dlp `WARNING:`/`ERROR:` output is now logged at the matching Serilog level instead of all being recorded as information.

### Fixed
- The MSI installed to `C:\Program Files\...` and required elevation; per-user redirection to `%LOCALAPPDATA%\Programs` needs the `ALLUSERS`/`MSIINSTALLPERUSER` MSI properties, which were dropped during the WiX 5 migration.
- Argument injection into yt-dlp: command lines are now built with `ArgumentList` and the URL is passed after `--` and validated as http(s).
- Tool paths (`yt-dlp`, `ffmpeg`) and `appsettings.json` resolve against the install directory instead of the process working directory.
- Concurrent downloader invocations no longer race on a shared `ProcessStartInfo`.
- History hyperlinks are scheme-checked before being handed to the shell.
- "Open folder" from history no longer throws (`Process.Start` on a directory path is unsupported on modern .NET).
- Result-path detection now understands modern yt-dlp output (`[Merger]`, "has already been downloaded").
- Cancelling a download during the metadata-fetch phase no longer leaves the UI permanently disabled.
- Playlist dumps with missing `ext`/`title`/`webpage_url` fields no longer break downloads after the System.Text.Json migration.

## [2.3.29] - 2023-06-05

Last release before the modernization effort; see the Git history for details.
