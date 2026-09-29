# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# MediaDownloader

WPF desktop front-end for the yt-dlp CLI (Windows, x64 only). Self-contained .NET 10, per-user WiX MSI installer.

## Repository Map

- `MediaDownloader/` — WPF exe. Generic Host composition in `App.xaml.cs`; `UI/ViewModels/MainWindowViewModel.cs` uses CommunityToolkit.Mvvm source generators (`[ObservableProperty]` partial properties, `[RelayCommand]`); UI-facing services in `Services/` (history, folders, shell, clipboard, storage initializer).
- `MediaDownloader.Download/` — yt-dlp wrapper library (no WPF deps; Serilog only): `Downloader` (process invocation), `DownloadManager` (orchestration/retries/progress), `Utilities/` (output parser, JSON mapper, URL validator).
- `MediaDownloader.Data/` — EF Core 10 + SQLite: `DataContext`, `Storage` (async API, `InitializeAsync` migrates + loads), `Migrations/`.
- `MediaDownloader.Tests/` — xUnit v3; fixtures with real yt-dlp `-J` shapes in `TestData/`.
- `Installers/MediaDownloaderSetup/` — SDK-style WiX 5 project (`Files` wildcard harvesting; a BeforeBuild target publishes the app into `publish/<Configuration>/`).
- `third-party/` — vendored `yt-dlp.exe`, `ffmpeg` and `quickjs/qjs.exe`, copied to build output by the app csproj (a recursive glob, so anything added here flows into the build output, publish, MSI and portable ZIP automatically); yt-dlp self-updates at app start.
  - yt-dlp needs an external JavaScript runtime for YouTube; only `deno` is enabled by default, so `Downloader` passes `--js-runtimes node` plus `--js-runtimes quickjs:<bundled path>`. Priority is deno > node > quickjs, so a user's own install wins and the bundled QuickJS is the fallback. Do not pass `--no-js-runtimes` — that would disable that preference.

## Commands

```powershell
dotnet build MediaDownloader/MediaDownloader.csproj -c Release -p:Platform=x64   # app only (what CI builds; fast)
dotnet build MediaDownloader.sln -c Release -p:Platform=x64                      # whole solution incl. MSI (runs a full self-contained publish)
dotnet test MediaDownloader.Tests/MediaDownloader.Tests.csproj -c Release -p:Platform=x64
dotnet test MediaDownloader.Tests/MediaDownloader.Tests.csproj -c Release -p:Platform=x64 --filter "FullyQualifiedName~DownloaderArgumentsTests"  # one class/test
dotnet build Installers/MediaDownloaderSetup/MediaDownloaderSetup.wixproj -c Release -p:Platform=x64  # -> out/MediaDownloaderSetup.msi

# EF migrations (local dotnet-ef tool; design-time factory takes the connection string after --)
dotnet dotnet-ef migrations add <Name> --project MediaDownloader.Data --startup-project MediaDownloader.Data -- "Data Source=dummy.db"
```

## Architecture

- **Startup**: `App` builds the host, logs go to the user-data folder, `DownloaderOptions` is bound from `appsettings.json` (the tool paths, relative to the exe) and validated on start. `StorageInitializer` (hosted service) migrates the DB and seeds the default folder *before* `MainWindow` is shown. If startup fails, a message box appears and the app exits with code 1.
- **Download pipeline**: `MainWindowViewModel` → `DownloadManager.DownloadItemAsync` → `Downloader`. There are two phases: (1) a `-J` metadata call that `DownloadItemMapper` turns into a `DownloadItem` with `Entries` (a playlist gets its own subfolder); (2) one yt-dlp download process per entry. Both phases retry (`DownloadRetriesNumber`). Progress is `IProgress<ProgressReportModel>`: the 0–100 range is split evenly across entries, and each stdout line is parsed by `DownloadOutputParser` (source-generated regexes). The same parser maps yt-dlp `WARNING:`/`ERROR:` lines to Serilog levels. Cancellation kills the yt-dlp process.
- **Format selection**: `DownloadFormatType` maps to yt-dlp `-f` strings stored in `MediaDownloader.Download/Properties/Resources.resx`, not in code.
- **Storage**: `Storage` is a DI singleton that holds one long-lived `DataContext`. The UI binds directly to `Local.ToObservableCollection()` views, so mutate data only through `Storage` methods. History is capped at 20 records and folders at 10; the oldest entry is evicted.
- **Tests** run on Microsoft Testing Platform, not VSTest: `global.json` sets `test.runner`, which xunit.v3 4.x requires on the .NET 10 SDK, so don't remove it. Besides `--filter`, xunit's own `--filter-class`/`--filter-method` also work. The tests cover only the two libraries (the WPF project is not referenced). `Downloader.Build*Arguments` are `internal` and tested through `InternalsVisibleTo`. `StorageTests` run real migrations against a temp SQLite file. No test launches yt-dlp.

## Conventions & Constraints

- x64 only — always pass `-p:Platform=x64`; solution has no AnyCPU configs.
- **Versioning is CalVer `YYYY.MM.DD`** (UTC): `Directory.Build.props` defaults `Version` to today's date; the release workflow passes an explicit `-p:Version` (with a `.N` suffix for repeated same-day releases). There is no NBGV/version.json. The MSI's ProductVersion is a derived `YY.M.D` (`-p:MsiVersion`) because MSI caps the version major at 255.
- **Branch flow**: `dev` is the default branch — create feature branches from `dev` and PR back into `dev`. Releasing = open a PR `dev` → `master`; merging it triggers `.github/workflows/release.yml`, which tags `vYYYY.MM.DD` and publishes the GitHub release (MSI + portable ZIP, auto-generated notes) with no manual steps. Never push to `master` directly.
- **Attribution**: never add a `Co-Authored-By: Claude` trailer to commits. PR descriptions and PR comments written by Claude may carry a marker (e.g. a "🤖 Generated with Claude Code" footer) so they can be told apart from ones written by a human.
- `CHANGELOG.md` follows Keep a Changelog; add user-visible changes under `[Unreleased]`.
- Shared build settings live in `Directory.Build.props` (conditioned to `.csproj` so the wixproj is unaffected).
- Process launches use `ProcessStartInfo.ArgumentList` only — never concatenate argument strings; user URLs are validated (http/https) and passed after `--`.
- All file paths resolve against `AppContext.BaseDirectory`, never the CWD (`appsettings.json`, tool paths).
- The WiX `Package/@UpgradeCode` in `Product.wxs` must never change.
- `Resources.Designer.cs` files are maintained by hand when editing resx outside Visual Studio (`dotnet build` does not regenerate them). Russian satellite resx files must be kept in sync.
- User data (SQLite DB, Serilog logs) lives in `%LOCALAPPDATA%\Wolfcub\Media Downloader`.
- The build is zero-warning (`AnalysisLevel=latest-recommended`); keep it that way.
