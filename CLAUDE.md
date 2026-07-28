# MediaDownloader

WPF desktop front-end for the yt-dlp CLI (Windows, x64 only). Self-contained .NET 10, per-user WiX MSI installer.

## Repository Map

- `MediaDownloader/` — WPF exe. Generic Host composition in `App.xaml.cs`; `UI/ViewModels/MainWindowViewModel.cs` uses CommunityToolkit.Mvvm source generators (`[ObservableProperty]` partial properties, `[RelayCommand]`); UI-facing services in `Services/` (history, folders, shell, clipboard, storage initializer).
- `MediaDownloader.Download/` — yt-dlp wrapper library (no WPF deps): `Downloader` (process invocation), `DownloadManager` (orchestration/retries/progress), `Utilities/` (output parser, JSON mapper, URL validator).
- `MediaDownloader.Data/` — EF Core 10 + SQLite: `DataContext`, `Storage` (async API, `InitializeAsync` migrates + loads), `Migrations/`.
- `MediaDownloader.Tests/` — xUnit v3; fixtures with real yt-dlp `-J` shapes in `TestData/`.
- `Installers/MediaDownloaderSetup/` — SDK-style WiX 5 project (`Files` wildcard harvesting; a BeforeBuild target publishes the app into `publish/<Configuration>/`).
- `third-party/` — vendored `yt-dlp.exe` and `ffmpeg`, copied to build output by the app csproj; yt-dlp self-updates at app start.

## Commands

```powershell
dotnet build MediaDownloader.sln -c Release -p:Platform=x64          # whole solution incl. MSI
dotnet test MediaDownloader.Tests/MediaDownloader.Tests.csproj -c Release -p:Platform=x64
dotnet build Installers/MediaDownloaderSetup/MediaDownloaderSetup.wixproj -c Release -p:Platform=x64  # -> out/MediaDownloaderSetup.msi

# EF migrations (local dotnet-ef tool; design-time factory takes the connection string after --)
dotnet dotnet-ef migrations add <Name> --project MediaDownloader.Data --startup-project MediaDownloader.Data -- "Data Source=dummy.db"
```

## Conventions & Constraints

- x64 only — always pass `-p:Platform=x64`; solution has no AnyCPU configs.
- **Versioning is CalVer `YYYY.MM.DD`** (UTC): `Directory.Build.props` defaults `Version` to today's date; the release workflow passes an explicit `-p:Version` (with a `.N` suffix for repeated same-day releases). There is no NBGV/version.json. The MSI's ProductVersion is a derived `YY.M.D` (`-p:MsiVersion`) because MSI caps the version major at 255.
- **Branch flow**: `dev` is the default branch — create feature branches from `dev` and PR back into `dev`. Releasing = open a PR `dev` → `master`; merging it triggers `.github/workflows/release.yml`, which tags `vYYYY.MM.DD` and publishes the GitHub release (MSI + portable ZIP, auto-generated notes) with no manual steps. Never push to `master` directly.
- Shared build settings live in `Directory.Build.props` (conditioned to `.csproj` so the wixproj is unaffected).
- Process launches use `ProcessStartInfo.ArgumentList` only — never concatenate argument strings; user URLs are validated (http/https) and passed after `--`.
- All file paths resolve against `AppContext.BaseDirectory`, never the CWD (`appsettings.json`, tool paths).
- The WiX `Package/@UpgradeCode` in `Product.wxs` must never change.
- `Resources.Designer.cs` files are maintained by hand when editing resx outside Visual Studio (`dotnet build` does not regenerate them). Russian satellite resx files must be kept in sync.
- User data (SQLite DB, Serilog logs) lives in `%LOCALAPPDATA%\Wolfcub\Media Downloader`.
- The build is zero-warning (`AnalysisLevel=latest-recommended`); keep it that way.
